pipeline {
    agent any
    environment {
        DOCKER_IMAGE = "hoangnhat2003/drinkshop-api"
        DOCKER_HUB_CREDS = 'docker-hub-credentials-id' 
    }
    stages {
        stage('Clone Code') {
            steps {
                checkout scm
            }
        }

        stage('Unit Test') {
            agent {
                docker { 
                    image 'mcr.microsoft.com/dotnet/sdk:9.0' 
                    reuseNode true
                }
            }
            steps {
                // Chạy test cho dự án Voucher và các service khác
                sh 'dotnet test DrinkShop.Tests/DrinkShop.Tests.csproj --logger "junit;LogFileName=test-results.xml"'
            }
            post {
                always {
                    // Hiển thị biểu đồ kết quả test
                    junit '**/test-results.xml'
                }
            }
        }

        stage('Build & Push Docker Image') {
            steps {
                script {
                    retry(3) {
                        docker.withRegistry('https://index.docker.io/v1/', "${DOCKER_HUB_CREDS}") {
                            def customImage = docker.build("${DOCKER_IMAGE}:${env.BUILD_NUMBER}")
                            customImage.push("latest")   // Dùng cho Prod (Cổng 80)
                            customImage.push("develop")  // Dùng cho Dev (Cổng 8081)
                            customImage.push("${env.BUILD_NUMBER}")
                        }
                    }
                }
            }
        }

        stage('Deploy Both Environments') {
            steps {
                script {
                    // Lấy mật khẩu và secret từ Jenkins Credentials
                    withCredentials([
                        string(credentialsId: 'drinkshop-db-password', variable: 'DB_PWD'),
                        string(credentialsId: 'drinkshop-minio-user', variable: 'MINIO_USER'),
                        string(credentialsId: 'drinkshop-minio-pass', variable: 'MINIO_PWD'),
                        string(credentialsId: 'drinkshop-jwt-secret', variable: 'JWT_SEC')
                    ]) {
                        // 1. TRIỂN KHAI BẢN DEVELOPMENT (Cổng 8081)
                        sh """
                            IMAGE_TAG=${env.BUILD_NUMBER} DB_PASSWORD=${DB_PWD} MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} JWT_SECRET=${JWT_SEC} \
                            ASPNETCORE_ENVIRONMENT=Development \
                            docker-compose -f docker-compose.yml up -d --build --remove-orphans
                        """

                        // 2. TRIỂN KHAI BẢN PRODUCTION (Cổng 80)
                        sh """
                            IMAGE_TAG=${env.BUILD_NUMBER} DB_PASSWORD=${DB_PWD} MINIO_ROOT_USER=${MINIO_USER} \
                            MINIO_ROOT_PASSWORD=${MINIO_PWD} JWT_SECRET=${JWT_SEC} \
                            ASPNETCORE_ENVIRONMENT=Production \
                            docker-compose -f docker-compose.prod.yml up -d --build --remove-orphans
                        """
                    }
                }
            }
        }

        stage('Cleanup') {
            steps {
                sh 'docker image prune -f'
            }
        }
    }
}