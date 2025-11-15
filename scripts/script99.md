cd $(git rev-parse --show-toplevel)

aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

DOTNET_PID=$(ps | grep 'dotnet run.*AskVantage\.AppHost\.csproj' | awk '{print $1}')
APPHOST_PID=$(pgrep -P $DOTNET_PID | head -1)
DCP_PID=$(ps -f | grep $APPHOST_PID | grep "dcp start-apiserver" | awk '{print $2}')
IMAGEAPI_PID=$(ps -f | grep -v grep | grep -v "dotnet run" | grep ImageApi | awk '{print $2}')
KUBECONFIG=$(ps -f -p $DCP_PID | awk -F'--kubeconfig ' '{print $2}' | awk '{print $1}' | sort -u | tr -d '\n') && echo $KUBECONFIG

kubectl --kubeconfig $KUBECONFIG apply -f ./scripts/container.yaml
kubectl --kubeconfig $KUBECONFIG get container redis
docker ps | grep redis:8.2