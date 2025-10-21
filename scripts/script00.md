pushd . && cd $(git rev-parse --show-toplevel)

## echo hello

echo 'Hello everyone!'

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

pushd . && cd $(git rev-parse --show-toplevel)

killall dotnet && clear && popd