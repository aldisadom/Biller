# Run the tests and collect code coverage
dotnet test tests/xUnitTests/xUnitTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura --collect:"XPlat Code Coverage" | Tee-Object -Variable output

# Extract the GUID from the test results path in the output
$guid = ($output | Select-String -Pattern "TestResults\\([a-zA-Z0-9\-]+)" | ForEach-Object { $_.Matches.Groups[1].Value })

# Generate the coverage report
reportgenerator -reports:"tests\xUnitTests\TestResults\$guid\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

Start-Process "coveragereport\index.html"
