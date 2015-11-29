# 2015Advent
Sample code for F# Advent post [Sharpen up your legacy apps performance](http://bohdanszymanik.blogspot.co.nz/2015/11/sharpen-up-your-legacy-apps-performance.html).

`Demo.fsx` holds the code.
`sampleBatches.txt` holds some sample data. 

Use Paket to get the dependencies and packages right, after paket's done its stuff use Visual Studio/Atom/Visual Studio Code/Emacs etc to run the script. If you've not used paket.exe before it's a two step process.

1. From a cmd prompt in the project directory do 
>`.paket\paket.bootstrapper.exe`
(This downloads the latest version of paket.exe.)

2. then
>`.paket\paket.exe install`
(This figures out and downloads all the dependencies. )

