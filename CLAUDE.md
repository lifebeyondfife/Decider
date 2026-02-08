# Decider - Claude Code Notes

## Tools
- Use MCP tools for GitHub operations (issues, PRs, etc.).

## Coding Conventions
- Always use explicit visibility identifiers (`private`, `public`, `internal`, `protected`) for all members

## Deploy
- To publish to nuget, update the Version XML value in `Integer/Csp.csproj` with the appropriate SEMVER increment

## Clean-up
- If you modify a file, update the year at the top of the file if necessary
- If you run a performance test, update the table in `Performance/README.md`
