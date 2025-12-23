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

        stage('Build & Push Docker Image') {
            steps {
                script {
                    docker.withRegistry('https://index.docker.io/v1/', DOCKER_HUB_CREDS) {
                        def customImage = docker.build("${DOCKER_IMAGE}:${BUILD_NUMBER}")
                        customImage.push()
                        customImage.push("latest")
                    }
                }
            }
        }

        stage('Deploy to Development') {
            steps {
                script {
                    // Lấy bí mật từ két sắt Jenkins gắn vào biến môi trường tạm thời
                    withCredentials([string(credentialsId: 'drinkshop-db-password', variable: 'DB_PWD')]) {
                        sh """
                            IMAGE_TAG=${BUILD_NUMBER} \
                            DB_PASSWORD=${DB_PWD} \
                            docker-compose up -d
                        """
                    }
                }
            }
        }

        stage('Deploy to Prod') {
            // Chỉ chạy stage này nếu bạn muốn deploy bản chính thức
            when { branch 'main' } 
            steps {
                script {
                    sh "IMAGE_TAG=${BUILD_NUMBER} docker-compose -f docker-compose.prod.yml up -d"
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