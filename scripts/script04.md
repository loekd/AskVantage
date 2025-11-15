# Service Location
clear
pushd . && cd $(git rev-parse --show-toplevel)

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN


open http://localhost:18888/
popd && killall dotnet && clear

