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
                    docker.withRegistry('https://index.docker.io/v1/', DOCKER_HUB_CREDS) {
                        def customImage = docker.build("${DOCKER_IMAGE}:${BUILD_NUMBER}")
                        customImage.push("latest") // Tag cho Prod
                        customImage.push("develop") // Tag cho Dev
                        customImage.push("${BUILD_NUMBER}")
                    }
                }
            }
        }

        stage('Deploy Both Environments') {
            steps {
                script {
                    withCredentials([
                        string(credentialsId: 'drinkshop-db-password', variable: 'DB_PWD'),
                        string(credentialsId: 'drinkshop-minio-user', variable: 'MINIO_USER'),
                        string(credentialsId: 'drinkshop-minio-pass', variable: 'MINIO_PWD'),
                        string(credentialsId: 'drinkshop-jwt-secret', variable: 'JWT_SEC')
                    ]) {
                        // 1. TRIỂN KHAI BẢN DEVELOPMENT (Cổng 8081)
                        def connStrDev = "Server=db;Database=CHTH;User Id=sa;Password=${DB_PWD};TrustServerCertificate=True;"
                        sh """
                            IMAGE_TAG=${BUILD_NUMBER} \
                            DB_PASSWORD=${DB_PWD} \
                            MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} \
                            JWT_SECRET=${JWT_SEC} \
                            MINIO_ENDPOINT=minio:9000 \
                            MINIO_BUCKET=drinkshop-bucket-dev \
                            ASPNETCORE_ENVIRONMENT=Development \
                            CONNECTION_STRING="${connStrDev}" \
                            docker-compose -f docker-compose.yml up -d --build
                        """

                        // 2. TRIỂN KHAI BẢN PRODUCTION (Cổng 80)
                        def connStrProd = "Server=db;Database=CHTH_Prod;User Id=sa;Password=${DB_PWD};TrustServerCertificate=True;"
                        sh """
                            IMAGE_TAG=${BUILD_NUMBER} \
                            DB_PASSWORD=${DB_PWD} \
                            MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} \
                            JWT_SECRET=${JWT_SEC} \
                            MINIO_ENDPOINT=minio:9000 \
                            MINIO_BUCKET=drinkshop-bucket-prod \
                            ASPNETCORE_ENVIRONMENT=Production \
                            CONNECTION_STRING="${connStrProd}" \
                            docker-compose -f docker-compose.prod.yml up -d --build
                        """
                    }
                }
            }
        }
    }
}