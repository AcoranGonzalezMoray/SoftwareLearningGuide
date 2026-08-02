#!/bin/sh
set -e

# Evita que AWS CLI intente descargar las URLs que empiezan por http://
export AWS_CLI_FILE_ENCODING=utf-8

# Si no hay endpoint configurado, asumimos MiniStack local
export AWS_ENDPOINT_URL="${AWS_ENDPOINT_URL:-http://localhost:4566}"

POOL_NAME="softwarelearningguide"
CLIENT_NAME="softwarelearningguide-app"
ADMIN_USER="admin@test.com"
NORMAL_USER="user@test.com"
ADMIN_PASSWORD="Test1234!"
NORMAL_PASSWORD="Test1234!"

echo "Seeding Cognito user pool '$POOL_NAME'..."

# --- User Pool (reutiliza el existente si lo hay; el Id es aleatorio en MiniStack) ---
POOL_ID=$(
  aws cognito-idp list-user-pools --max-results 50 \
    --query "UserPools[?Name=='$POOL_NAME'].Id | [0]" --output text
)

if [ -z "$POOL_ID" ] || [ "$POOL_ID" = "None" ]; then
  POOL_ID=$(
    aws cognito-idp create-user-pool --pool-name "$POOL_NAME" \
      --username-attributes email \
      --query "UserPool.Id" --output text
  )
  echo "  User pool creado: $POOL_ID"
else
  echo "  User pool existente: $POOL_ID"
fi

# --- App Client (sin secret, para el flujo USER_PASSWORD_AUTH) ---
CLIENT_ID=$(
  aws cognito-idp create-user-pool-client \
    --user-pool-id "$POOL_ID" \
    --client-name "$CLIENT_NAME" \
    --no-generate-secret \
    --explicit-auth-flows ALLOW_USER_PASSWORD_AUTH ALLOW_REFRESH_TOKEN_AUTH \
    --query "UserPoolClient.ClientId" --output text
)
echo "  App client: $CLIENT_ID"

# --- Grupos (admin y normal) ---
aws cognito-idp create-group --user-pool-id "$POOL_ID" --group-name admin \
  >/dev/null 2>&1 || echo "  Grupo 'admin' ya existía"
aws cognito-idp create-group --user-pool-id "$POOL_ID" --group-name normal \
  >/dev/null 2>&1 || echo "  Grupo 'normal' ya existía"

# --- Usuarios (idempotente: si ya existen, se resetea la contraseña) ---
aws cognito-idp admin-create-user \
  --user-pool-id "$POOL_ID" \
  --username "$ADMIN_USER" \
  --temporary-password "$ADMIN_PASSWORD" \
  --message-action SUPPRESS \
  --user-attributes Name=email,Value=$ADMIN_USER Name=email_verified,Value=true \
  >/dev/null 2>&1 || echo "  Usuario $ADMIN_USER ya existía"
aws cognito-idp admin-set-user-password \
  --user-pool-id "$POOL_ID" \
  --username "$ADMIN_USER" \
  --password "$ADMIN_PASSWORD" \
  --permanent

aws cognito-idp admin-create-user \
  --user-pool-id "$POOL_ID" \
  --username "$NORMAL_USER" \
  --temporary-password "$NORMAL_PASSWORD" \
  --message-action SUPPRESS \
  --user-attributes Name=email,Value=$NORMAL_USER Name=email_verified,Value=true \
  >/dev/null 2>&1 || echo "  Usuario $NORMAL_USER ya existía"
aws cognito-idp admin-set-user-password \
  --user-pool-id "$POOL_ID" \
  --username "$NORMAL_USER" \
  --password "$NORMAL_PASSWORD" \
  --permanent

# --- Asignar usuarios a grupos ---
aws cognito-idp admin-add-user-to-group \
  --user-pool-id "$POOL_ID" --username "$ADMIN_USER" --group-name admin \
  || echo "  $ADMIN_USER ya pertenece a 'admin'"
aws cognito-idp admin-add-user-to-group \
  --user-pool-id "$POOL_ID" --username "$NORMAL_USER" --group-name normal \
  || echo "  $NORMAL_USER ya pertenece a 'normal'"

# --- Persistir UserPoolId/ClientId en SSM (dinámicos en MiniStack) ---
aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/UserPoolId" \
  --value "$POOL_ID" --type String --overwrite

aws ssm put-parameter \
  --name "/softwarelearningguide/dev/api/CloudProvidersConfigurations/AWS/Cognito/ClientId" \
  --value "$CLIENT_ID" --type String --overwrite

echo ""
echo "Cognito listo:"
echo "  UserPoolId: $POOL_ID"
echo "  ClientId:   $CLIENT_ID"
echo "  Admin:      $ADMIN_USER / $ADMIN_PASSWORD (grupo admin)"
echo "  Normal:     $NORMAL_USER / $NORMAL_PASSWORD (grupo normal)"
echo ""
echo "Para obtener un token:"
echo "  aws cognito-idp initiate-auth --client-id $CLIENT_ID \\"
echo "    --auth-flow USER_PASSWORD_AUTH \\"
echo "    --auth-parameters USERNAME=$ADMIN_USER,PASSWORD=$ADMIN_PASSWORD"
