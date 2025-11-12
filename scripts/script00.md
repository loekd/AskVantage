pushd . && cd $(git rev-parse --show-toplevel)

## echo hello

echo 'Hello from ScriptRunner!'

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

killall dotnet && clear && popd