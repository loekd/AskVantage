pushd . && cd $(git rev-parse --show-toplevel)

## echo hello

#echo 'Hello from ScriptRunner!'

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

#show frontend
open https://localhost:7207

#show aspire dashboard
open https://localhost:18888

#cleanup
killall dotnet && clear && popd