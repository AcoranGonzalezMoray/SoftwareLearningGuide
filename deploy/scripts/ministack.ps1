# =========================================================
# ministack.ps1 - AWS CLI para MiniStack (emulador AWS local)
#
# MiniStack corre en docker-compose en el puerto 4566 y emula
# SSM Parameter Store, SNS, SQS y Cognito. La AWS CLI necesita
# saber que debe apuntar a ese endpoint (MiniStack acepta cualquier
# credencial; por convenio se usan "test"/"test").
#
# Acciones:
#   Configure   Crea el perfil "ministack" en la AWS CLI
#               (AccessKeyId=test, SecretAccessKey=test,
#               region=us-east-1, endpoint_url en AWS CLI v2)
#   Verify      Comprueba que SSM/Cognito/SNS/SQS responden y que
#               los parametros de los 3 servicios estan sembrados
#   Reseed      Re-escribe los parametros SSM cuyo valor depende del
#               host (idempotente; util tras reiniciar MiniStack)
#   Kubeconfig  Prepara kubectl para el EKS emulado (k3s): extrae el
#               kubeconfig admin del contenedor k3s de MiniStack y lo
#               instala en el contexto slg-minstack de ~/.kube/config
#               (reemplaza el exec "aws get-token", que k3s no valida)
#   CoreDns     Parchea el CoreDNS del k3s para que los PODS resuelvan
#               'host.docker.internal' (NodeHosts -> 192.168.65.254) y
#               forwardee DNS externo por 192.168.65.7 / 8.8.8.8. Sin este
#               parche las apps no pueden conectar con SQL/RabbitMQ/MiniStack
#               que corren en el host. Idempotente; necesario tras cada
#               'terraform destroy/apply' (el ConfigMap vuelve al default).
#   Prepare     Kubeconfig + Reseed(-AppHost) + CoreDns: lo que hay que
#               ejecutar SIEMPRE despues de crear el cluster (terraform apply).
#   All         Configure + Verify + Reseed  (por defecto)
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action All
#   powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Verify
#   powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Kubeconfig
#   powershell -ExecutionPolicy Bypass -File .\deploy\scripts\ministack.ps1 -Action Prepare -AppHost host.docker.internal
#
# Requisitos:
#   - docker-compose up -d ministack redis   (MiniStack corriendo)
#   - AWS CLI v2 instalada  (winget install Amazon.AWSCLI)
#   - kubectl para las acciones Kubeconfig/CoreDns/Prepare  (winget install Kubernetes.kubectl)
#
# Las credenciales "test"/"test" solo sirven contra MiniStack;
# este script NO toca AWS real.
# =========================================================

[CmdletBinding()]
param(
    [ValidateSet("Configure", "Verify", "Reseed", "Kubeconfig", "CoreDns", "Prepare", "All")]
    [string]$Action = "All",

    [string]$Endpoint = "http://localhost:4566",

    [string]$AppHost = "localhost"
)

$ErrorActionPreference = "Stop"
$Profile = "ministack"

function Test-AwsCli {
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI no esta instalada. Instala AWS CLI v2: winget install Amazon.AWSCLI"
    }
}

function Invoke-Aws {
    param([string[]]$Cmd)
    & aws --profile $Profile --endpoint-url $Endpoint @Cmd
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI fallo (exit $LASTEXITCODE): aws $($Cmd -join ' ')"
    }
}

function Invoke-Configure {
    Write-Host "==> Configurando perfil '$Profile' apuntando a MiniStack ($Endpoint)..." -ForegroundColor Cyan
    aws configure set aws_access_key_id test --profile $Profile
    aws configure set aws_secret_access_key test --profile $Profile
    aws configure set region us-east-1 --profile $Profile
    # endpoint_url por perfil solo existe en AWS CLI v2; con v1 es obligatorio pasar --endpoint-url
    aws configure set endpoint_url $Endpoint --profile $Profile

    Write-Host ""
    Write-Host "Perfil '$Profile' listo. Ejemplos de uso:" -ForegroundColor Green
    Write-Host "  aws --profile $Profile --endpoint-url $Endpoint ssm get-parameters-by-path --path '/softwarelearningguide/' --recursive"
    Write-Host "  aws --profile $Profile --endpoint-url $Endpoint cognito-idp list-user-pools --max-results 50"
    Write-Host "  aws --profile $Profile --endpoint-url $Endpoint sns list-topics"
    Write-Host "  aws --profile $Profile --endpoint-url $Endpoint sqs list-queues"
}

function Invoke-Verify {
    Write-Host "==> Verificando MiniStack en $Endpoint..." -ForegroundColor Cyan

    Write-Host ""
    Write-Host "-- SSM: parametros sembrados por servicio --"
    foreach ($path in @("/softwarelearningguide/dev/api/",
                        "/softwarelearningguide/dev/outboxprocessor/",
                        "/softwarelearningguide/dev/consumer/")) {
        $params = Invoke-Aws @("ssm", "get-parameters-by-path",
                               "--path", $path,
                               "--query", "Parameters[].Name",
                               "--output", "text")
        Write-Host "  $path"
        if ($params) {
            ($params.Trim() -split "`t") | ForEach-Object { Write-Host "    $_" }
        } else {
            Write-Host "    (vacio - ejecuta Reseed)"
        }
    }

    Write-Host ""
    Write-Host "-- Cognito (user pools) --"
    Invoke-Aws @("cognito-idp", "list-user-pools", "--max-results", "50",
                 "--query", "UserPools[].[Id,Name]", "--output", "table") | Write-Host

    Write-Host ""
    Write-Host "-- SNS topics / SQS queues --"
    $topics = Invoke-Aws @("sns", "list-topics", "--output", "text")
    $queues = Invoke-Aws @("sqs", "list-queues", "--output", "text")
    Write-Host "  Topics: $(if ($topics) { $topics } else { '(ninguno)' })"
    Write-Host "  Queues: $(if ($queues) { $queues } else { '(ninguna)' })"
}

function Invoke-Reseed {
    Write-Host "==> Re-sembrando SSM parametros dependientes del host ('$AppHost')..." -ForegroundColor Cyan

    $sql = "Server=$AppHost,1433;Database=SoftwareLearningGuide;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"

    $params = @(
        # --- API ---
        @{ Name = "/softwarelearningguide/dev/api/ConnectionStrings/SoftwareLearningGuide"; Value = $sql },
        @{ Name = "/softwarelearningguide/dev/api/OpenTelemetry/Otlp/Endpoint"; Value = "http://$AppHost`:4317" },
        @{ Name = "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/ServiceUrl"; Value = "http://$AppHost`:4566" },
        # --- OutboxProcessor ---
        @{ Name = "/softwarelearningguide/dev/outboxprocessor/Database/SoftwareLearningGuide"; Value = $sql },
        @{ Name = "/softwarelearningguide/dev/outboxprocessor/MessageBroker/Host"; Value = $AppHost },
        @{ Name = "/softwarelearningguide/dev/outboxprocessor/OpenTelemetry/Otlp/Endpoint"; Value = "http://$AppHost`:4317" },
        @{ Name = "/softwarelearningguide/dev/outboxprocessor/CloudProvidersConfigurations/AWS/Messaging/ServiceUrl"; Value = "http://$AppHost`:4566" },
        # --- Consumer ---
        @{ Name = "/softwarelearningguide/dev/consumer/MessageBroker/Host"; Value = $AppHost },
        @{ Name = "/softwarelearningguide/dev/consumer/OpenTelemetry/Otlp/Endpoint"; Value = "http://$AppHost`:4317" },
        @{ Name = "/softwarelearningguide/dev/consumer/CloudProvidersConfigurations/AWS/Messaging/ServiceUrl"; Value = "http://$AppHost`:4566" }
    )

    foreach ($p in $params) {
        $name  = $p["Name"]
        Invoke-Aws @("ssm", "put-parameter",
                     "--name", $name,
                     "--value", $p["Value"],
                     "--type", "String",
                     "--overwrite") | Out-Null
        Write-Host "  - actualizado: $name"
    }
}

function Invoke-Kubeconfig {
    Write-Host "==> Preparando kubectl para el EKS emulado (k3s)..." -ForegroundColor Cyan

    $cont = docker ps --filter "name=ministack-eks-" --format "{{.Names}}" | Select-Object -First 1
    if (-not $cont) {
        throw "No se encontro el contenedor k3s de MiniStack ('ministack-eks-*'). Ejecuta primero: terraform apply con terraform.minstack.tfvars.example"
    }
    Write-Host "  Contenedor k3s: $cont"

    $ports = docker port $cont "6443/tcp" | Select-Object -First 1
    if (-not $ports) {
        throw "El contenedor $cont no publica el puerto 6443/tcp."
    }
    $hostPort = ($ports -split ":")[-1]

    $kubeDir = Join-Path $HOME ".kube"
    if (-not (Test-Path $kubeDir)) { New-Item -ItemType Directory -Path $kubeDir | Out-Null }
    $k3sFile = Join-Path $kubeDir "k3s-minstack.yaml"

    $yamlLines = @(docker exec $cont cat /etc/rancher/k3s/k3s.yaml)
    if (-not $yamlLines) {
        throw "No se pudo leer /etc/rancher/k3s/k3s.yaml del contenedor $cont."
    }
    $yaml = ($yamlLines -join "`n") -replace "https://127.0.0.1:6443", "https://localhost:$hostPort"
    [IO.File]::WriteAllText($k3sFile, $yaml, [Text.Encoding]::ASCII)   # WriteAllText (share Read): Set-Content falla si otro proceso tiene el archivo abierto
    Write-Host "  Kubeconfig admin k3s escrito en: $k3sFile"

    $main = Join-Path $kubeDir "config"

    if (-not (Test-Path $main) -or -not ((Get-Content -Raw $main) -match "slg-minstack")) {
        # El contexto slg-minstack aun no existe: fusiona el kubeconfig admin en ~/.kube/config
        if (Get-Command kubectl -ErrorAction SilentlyContinue) {
            $env:KUBECONFIG = "$main;$k3sFile"
            $flattened = (kubectl config view --flatten -o yaml) -join "`n"
            $env:KUBECONFIG = $null
            if ($flattened) {
                [IO.File]::WriteAllText($main, $flattened, [Text.Encoding]::ASCII)
                kubectl config use-context default | Out-Null
                Write-Host "  Contexto k3s 'default' anadido a $main y activado."
            } else {
                Write-Host "  Aviso: no se pudo fusionar. Usa el archivo: $k3sFile"
            }
        } else {
            Write-Host "  Aviso: no se encontro kubectl para fusionar. Usa el archivo: $k3sFile"
        }
        return
    }

    # El contexto slg-minstack ya existe: refrescalo SIEMPRE. Al recrearse el
    # contenedor k3s, Docker puede asignarle otro puerto host, dejando el
    # server/CA/cliente de ~/.kube/config obsoletos.
    $c = Get-Content -Raw $main
    $ctx = [regex]::Match($c, '(?s)- context:\s+cluster:\s*([^\r\n]+)\r?\n\s+user:\s*([^\r\n]+)\r?\n\s+name:\s+slg-minstack')
    if (-not $ctx.Success) {
        Write-Host "  Aviso: no se pudo leer el bloque del contexto slg-minstack en $main. Usa el archivo: $k3sFile"
        return
    }
    $cluster = $ctx.Groups[1].Value.Trim([char[]]@(' ', '"', "'"))
    $user    = $ctx.Groups[2].Value.Trim([char[]]@(' ', '"', "'"))

    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        Write-Host "  Aviso: kubectl no disponible para actualizar $main. Usa el archivo: $k3sFile"
        return
    }

    $ca   = ([regex]::Match($yaml, 'certificate-authority-data:\s*(\S+)')).Groups[1].Value
    $cert = ([regex]::Match($yaml, 'client-certificate-data:\s*(\S+)')).Groups[1].Value
    $key  = ([regex]::Match($yaml, 'client-key-data:\s*(\S+)')).Groups[1].Value

    # 1) Server y CA del cluster (el puerto host cambia al recrear el contenedor k3s)
    $tmp = Join-Path $env:TEMP "k3s-ca-$PID.pem"
    [IO.File]::WriteAllBytes($tmp, [Convert]::FromBase64String($ca))
    kubectl --kubeconfig $main config set-cluster $cluster --server "https://localhost:$hostPort" --certificate-authority $tmp --embed-certs=true | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Remove-Item $tmp -ErrorAction SilentlyContinue
        throw "kubectl config set-cluster fallo (exit $LASTEXITCODE)."
    }
    Remove-Item $tmp -ErrorAction SilentlyContinue
    Write-Host "  Cluster '$cluster' -> https://localhost:$hostPort (server + CA renovados)."

    # 2) Cliente admin k3s: quita el exec 'aws get-token' si existe y fija los certs nuevos
    $pat = '    exec:\r?\n      apiVersion: client\.authentication\.k8s\.io/v1beta1\r?\n      args:\r?\n      - --region\r?\n      - \S+\r?\n      - eks\r?\n      - get-token\r?\n      - --cluster-name\r?\n      - \S+\r?\n      - --output\r?\n      - json\r?\n      command: aws\r?\n      env:\r?\n      - name: AWS_PROFILE\r?\n        value: ministack'
    $nameRe = '- name:\s*["'']?' + [regex]::Escape($user) + '["'']?\r?\n\s+user:'
    $block = [regex]::Match($c, '(?s)' + $nameRe + '.*?(?=\r?\n- name:|\r?\n\s{0,2}clusters:|\r?\n\s{0,2}contexts:|\r?\n\s{0,2}users:|\r?\n\s{0,2}kind:|\r?\napiVersion:|\Z)')

    if ($block.Value -match $pat) {
        $userText = $block.Value -replace '(?m)^[ \t]+client-(certificate|key)-data:[ \t]*\S+[ \t]*\r?\n', ''
        $newUser  = $userText -replace $pat, "    client-certificate-data: $cert`n    client-key-data: $key"
        [IO.File]::WriteAllText($main, $c.Replace($block.Value, $newUser), [Text.Encoding]::ASCII)
        Write-Host "  Usuario '$user' parcheado con el cliente admin k3s (exec aws get-token eliminado)."
    } else {
        kubectl --kubeconfig $main config set "users.$user.client-certificate-data" $cert | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "kubectl config set client-certificate-data fallo." }
        kubectl --kubeconfig $main config set "users.$user.client-key-data" $key | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "kubectl config set client-key-data fallo." }
        Write-Host "  Usuario '$user' actualizado con el cliente admin k3s."
    }
}

function Invoke-CoreDns {
    Write-Host "==> Parcheando CoreDNS del k3s (resolucion de host.docker.internal en los pods)..." -ForegroundColor Cyan

    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        throw "kubectl no disponible. Instala kubectl: winget install Kubernetes.kubectl"
    }

    $cm = kubectl -n kube-system get cm coredns -o json | ConvertFrom-Json
    if (-not $cm.data) {
        throw "No se encontro el ConfigMap 'coredns'. Ejecuta primero: terraform apply con terraform.minstack.tfvars.example"
    }

    $corefile = $cm.data.Corefile
    if ($corefile -notmatch 'forward \. /etc/resolv\.conf') {
        Write-Host "  Corefile ya parcheado (forward externo)."
    } else {
        $corefile = $corefile -replace 'forward \. /etc/resolv\.conf', 'forward . 192.168.65.7 8.8.8.8 1.1.1.1'
        Write-Host "  Corefile: 'forward . /etc/resolv.conf' -> '192.168.65.7 8.8.8.8 1.1.1.1'"
    }

    $nodehosts = $cm.data.NodeHosts
    if ($nodehosts -match 'host\.docker\.internal') {
        Write-Host "  NodeHosts ya contiene host.docker.internal."
    } else {
        $nodehosts = $nodehosts.TrimEnd() + "`n192.168.65.254 host.docker.internal`n"
        Write-Host "  NodeHosts: anadido '192.168.65.254 host.docker.internal'."
    }

    $new = [ordered]@{
        apiVersion = "v1"
        kind       = "ConfigMap"
        metadata   = @{ name = "coredns"; namespace = "kube-system" }
        data       = [ordered]@{ Corefile = $corefile; NodeHosts = $nodehosts }
    }
    $tmp = Join-Path $env:TEMP "coredns-patch.yaml"
    ($new | ConvertTo-Json -Depth 6) | Set-Content -Path $tmp -Encoding ascii
    kubectl apply -f $tmp | Out-Null
    Remove-Item $tmp -ErrorAction SilentlyContinue
    if ($LASTEXITCODE -ne 0) {
        throw "kubectl apply fallo (exit $LASTEXITCODE)."
    }

    kubectl -n kube-system rollout restart deploy/coredns | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "kubectl rollout restart deploy/coredns fallo (exit $LASTEXITCODE)."
    }
    kubectl -n kube-system rollout status deploy/coredns --timeout=120s
}

if ($Action -eq "Kubeconfig") {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "Docker no accesible. Asegurate de que Docker Desktop esta arrancado."
    }
} else {
    Test-AwsCli
}

switch ($Action) {
    "Configure"  { Invoke-Configure }
    "Verify"     { Invoke-Verify }
    "Reseed"     { Invoke-Reseed }
    "Kubeconfig" { Invoke-Kubeconfig }
    "CoreDns"    { Invoke-CoreDns }
    "Prepare"    { Invoke-Kubeconfig; Invoke-Reseed; Invoke-CoreDns }
    "All"        { Invoke-Configure; Invoke-Verify; Invoke-Reseed }
}

Write-Host ""
Write-Host "Listo." -ForegroundColor Green