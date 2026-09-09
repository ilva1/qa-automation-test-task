using NUnit.Framework;

// How many tests run at once. Kept modest because each UI test owns a browser;
// on CI this is raised, or the UI suite is pointed at a grid via SeleniumRemoteUrl.
[assembly: LevelOfParallelism(3)]
