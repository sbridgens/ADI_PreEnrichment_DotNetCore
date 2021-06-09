@Library('jenkins-pipeline') _

properties([
	parameters([string(name: 'VERSION', defaultValue: '')])
])

node {
	def gitCommit
	def name = "pre-enricher"
	def environment = k8s.environmentFromString("aws-appdev")
	def namespaces = "development"
	def registry = "registry.lgi.io/libertyglobal"

	stage("Checkout") {
		gitCommit = checkout(scm).GIT_COMMIT
	}

	stage("Build Docker Images") {
		for (e in ['service','generator', 'tracking']) {
		sh "cp -r model $e/ && cp -r $e /tmp/ && mv /tmp/$e $e/"
		}
		service = docker.build("${registry}/${name}-service:${gitCommit}", "./service")
		generator = docker.build("${registry}/${name}-generator:${gitCommit}", "./generator")
		tracking = docker.build("${registry}/${name}-tracking:${gitCommit}", "./tracking")
	}

	stage("Build Helm Package") {

		if (params.VERSION) {
			for (e in ['helm/','helm/charts/service', 'helm/charts/generator', 'helm/charts/tracking']){
				sh "sed -i 's|^version:.*|version: $params.VERSION|g' $e/Chart.yaml"
				sh "sed -i 's|^appVersion:.*|appVersion: $params.VERSION|g' $e/Chart.yaml"
			}
		}

		docker.image('registry.lgi.io/libertyglobal/helm:3.2.0').inside {
			for (e in ['service','generator', 'tracking']) {
				sh "helm dep up helm/charts/$e"
			}
				sh 'helm package helm -u -d target'
		}
	}

	if (params.VERSION) {

		stage("Push to Registry") {
			for (app in [service, generator, tracking]) {
				app.push "${gitCommit}"
          		app.push "${params.VERSION}"
			}
		}

		stage("Upload Helm Chart") {
			findFiles(glob: 'target/*.tgz').each { f->
				withCredentials([usernamePassword(credentialsId: 'cto-webappdev-jenkins', passwordVariable: 'HELM_REPO_PASSWORD', usernameVariable: 'HELM_REPO_USERNAME')]) {
            		sh "curl --fail -v -u ${HELM_REPO_USERNAME}:${HELM_REPO_PASSWORD} -T ${f.path} https://artifactory.tools.appdev.io/artifactory/helm/${f.name}"
        		}
			}
		}
	}

	if (env.BRANCH_NAME=="master" && params.VERSION) {

		stage("Push latest to Registry") {
			for (app in [service, generator, tracking]) {
				app.push "latest"
			}
		}

		stage ('Tag') {
			sshagent(['cto-webappdev-jenkins-key']) {
				sh("git tag -a ${params.VERSION} -m ''")
				sh "git push origin ${params.VERSION}"
			}
		}
	}
}
