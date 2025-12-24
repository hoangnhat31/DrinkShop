pipeline {
    agent any
    environment {
        DOCKER_IMAGE = "hoangnhat2003/drinkshop-api"
        DOCKER_HUB_CREDS = 'docker-hub-credentials-id'
    }
    stages {
        stage('Clone Code') {
            steps { checkout scm }
        }

        stage('Build & Push Docker Image') {
            steps {
                script {
                    // Gán tag dựa trên nhánh: main -> latest, develop -> develop
                    def tag = (env.BRANCH_NAME == 'main') ? "latest" : "develop"
                    docker.withRegistry('https://index.docker.io/v1/', DOCKER_HUB_CREDS) {
                        def customImage = docker.build("${DOCKER_IMAGE}:${BUILD_NUMBER}")
                        customImage.push("${tag}")
                        customImage.push("${BUILD_NUMBER}")
                    }
                }
            }
        }

        stage('Deploy') {
            steps {
                script {
                    def isProd = (env.BRANCH_NAME == 'main')
                    def composeFile = isProd ? "docker-compose.prod.yml" : "docker-compose.yml"
                    def bucketName = isProd ? "drinkshop-bucket-prod" : "drinkshop-bucket-dev"
                    def envMode = isProd ? "Production" : "Development"

                    withCredentials([
                        string(credentialsId: 'drinkshop-db-password', variable: 'DB_PWD'),
                        string(credentialsId: 'drinkshop-minio-user', variable: 'MINIO_USER'),
                        string(credentialsId: 'drinkshop-minio-pass', variable: 'MINIO_PWD'),
                        string(credentialsId: 'drinkshop-jwt-secret', variable: 'JWT_SEC')
                    ]) {
                        // Lưu ý: Bản Dev trong file compose bạn đã đổi cổng db thành 1434
                        def connStr = "Server=db;Database=DrinkShopDb;User Id=sa;Password=${DB_PWD};TrustServerCertificate=True;"
                        
                        sh """
                            IMAGE_TAG=${BUILD_NUMBER} \
                            DB_PASSWORD=${DB_PWD} \
                            MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} \
                            JWT_SECRET=${JWT_SEC} \
                            MINIO_ENDPOINT=minio:9000 \
                            MINIO_BUCKET=${bucketName} \
                            MINIO_USE_SSL=false \
                            ASPNETCORE_ENVIRONMENT=${envMode} \
                            CONNECTION_STRING="${connStr}" \
                            docker-compose -f ${composeFile} up -d --build
                        """
                    }
                }
            }
        }
    }
}