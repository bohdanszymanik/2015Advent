#load "packages/FsLab/FsLab.fsx"

open System
open System.IO
open System.Text.RegularExpressions
open System.Globalization
open Deedle

(*
    Worked example using a simulated batch timing data
    Each batch has 
        an identifier
        a step start and end datetime and step identifier

    For the example we'll always just have 4 steps
*)

let l1 = File.ReadAllText(__SOURCE_DIRECTORY__ + "\\sampleBatches.txt")

// use a regex to remove the batch identifier
let l2 = Regex.Replace(l1, @"(?m)^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}.*$","")


let stringToStream (s:string) =
    new MemoryStream(Text.Encoding.UTF8.GetBytes(s))

// load up the step timing data into a Deedle frame in memory
let batchSteps = Frame.ReadCsv(stringToStream(l2), 
                                separators=",", 
                                hasHeaders=false, 
                                schema="Start, End, Step#",
                                culture = "en-NZ")

// calculate the duration for each step and add as another column
batchSteps?Duration <- batchSteps 
    |> Frame.mapRowValues (fun row ->
                                    let timespan = row.GetAs<DateTime>("End") - row.GetAs<DateTime>("Start")
                                    timespan.TotalSeconds
                            )

// average step duration across all steps, answer 10.95s
Stats.mean batchSteps?Duration

// which step takes longest? answer step 2 at 20.97s
batchSteps.GroupRowsBy<string> "Step#"
|> Frame.getNumericCols
|> Series.mapValues (Stats.levelMean fst)

// since it's the longest, let's look at the duration distribution for step 2
let step2 =
    let (s:Series<_, float>) =
        batchSteps
        |> Frame.filterRowValues(fun row -> row.GetAs<string>("Step#") = "Step 2")
        |> Frame.getCol "Duration"
    s |> Series.values |> Array.ofSeq

step2 |> Array.max // max is 228

#r @"packages\Accord\lib\net45\Accord.dll"
#r @"packages\Accord.Math\lib\net45\Accord.Math.dll"
#r @"packages\Accord.Statistics\lib\net45\Accord.Statistics.dll"
#r @"packages\Accord.MachineLearning\lib\net45\Accord.MachineLearning.dll"

open Accord
open Accord.Math
open Accord.Statistics

let h = Visualizations.Histogram()
h.Compute(step2 , 100) // you get an odd double peak thing going on

h.Bins |> Seq.map(fun b -> b.Value)
|> List.ofSeq

open XPlot.GoogleCharts
open XPlot.GoogleCharts.Deedle
let histUpperEdges = h.Edges.[1..]
let histCnts = h.Values

let hists = Array.zip histUpperEdges histCnts
hists |> Chart.Column

// what sort of distribution might be an approximate fit?
let da = new Analysis.DistributionAnalysis(step2)
da.Compute()
da.GoodnessOfFit.[0] // Gamma
da.GoodnessOfFit.[1] // Normal
da.GoodnessOfFit.[2] // 3rd place goes to Poisson

// What if we wanted to simulate data?
// We can re-sample from within Deedle - refer to http://bluemountaincapital.github.io/Deedle/series.html
// Or we can generate from within Accord using
let gamma = Accord.Statistics.Distributions.Univariate.GammaDistribution.Estimate(step2)
gamma.Generate(1000)
|> Array.mapi (fun i s -> (string)i, s) 
|> Chart.Histogram

// what's the probability we're going to be under 100 based upon the estimated distribution?
gamma.DistributionFunction(100.) //99.7%

// the likelihood we could experience > 50s duration based upon estimated distribution?
gamma.ComplementaryDistributionFunction(50.) //6.8%