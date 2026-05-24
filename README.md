# Policy Portfolio Assessment

Refer to **Candidate Brief.docx** for the full requirements.
This README is just a quick orientation to the skeleton.

## Prerequisites

- .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
- Any IDE: Visual Studio, Rider, or VS Code

## Project layout

```
PolicyPortfolio.sln
├── src/
│   ├── PolicyPortfolio/             ← class library (implement here)
│   └── PolicyPortfolio.Console/     ← console driver (wire up here)
├── tests/
│   └── PolicyPortfolio.Tests/       ← xUnit tests (write here)
└── data/
    └── sample-portfolio.json        ← sample input
```

## Build and run

```
dotnet build
dotnet test
dotnet run --project src/PolicyPortfolio.Console -- data/sample-portfolio.json
```

## Submission

- Submit your work as a Git repository (zip the folder including the `.git/`
  directory). Your commit history is part of what we look at.
- Do NOT include `bin/` or `obj/` folders.
- A short `NOTES.md` describing any assumptions you made and anything you'd
  do differently with more time is welcome but not required.
