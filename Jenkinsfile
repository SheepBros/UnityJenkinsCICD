pipeline {
    agent none
    
    environment {
        UNITY_WIN_EXE = 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe'
        UNITY_WIN_LOG = 'unity-build.log'
        
        UNITY_MAC_EXE = '/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity'
        UNITY_IOS_LOG = 'unity-ios-build.log'
        XCODE_DEVELOPER = '/Applications/Xcode-26.app/Contents/Developer'
        XCODE_ARCHIVE_LOG = 'xcode-archive.log'
        XCODE_EXPORT_LOG = 'xcode-export.log'
        
        TEST_RESULTS_DIR = 'TestResults'
        EDITMODE_RESULTS = 'editmode-results.xml'
        EDITMODE_LOG= 'editmode-tests.log'
        
        PLAYMODE_RESULTS = 'playmode-results.xml'
        PLAYMODE_LOG = 'playmode-tests.log'
    }

    parameters {
        string(
            name: 'VERSION',
            defaultValue: '1.0.0',
            description: 'App version for manual builds. Example: 1.2.3.'
        )
        
        choice(
            name: 'PLATFORM',
            choices: ['All', 'Android', 'iOS'],
            description: 'Platforms to build'
        )
    
        choice(
            name: 'ENVIRONMENT',
            choices: ['Dev', 'Prod'],
            description: 'Runtime environment'
        )
        
        choice(
            name: 'CONFIGURATION',
            choices: ['Development', 'Release'],
            description: 'Unity build configuration'
        )
        
        choice(
            name: 'PACKAGE_FORMAT',
            choices: ['apk', 'aab'],
            description: 'Android package format'
        )
    }
    
    options {
        skipDefaultCheckout(true)
        
        disableConcurrentBuilds(abortPrevious: true)
        
        timestamps()
        
        timeout(
            time: 30,
            unit: 'MINUTES'
        )
        
        buildDiscarder(
            logRotator(
                numToKeepStr: '20',
                artifactNumToKeepStr: '10'
            )
        )
    }
    
    stages {
        stage('Windows CI') {
            agent {
                label 'unity-win'
            }
            
            stages {
                stage('Checkout') {
                    steps {
                        checkout scm
                    }
                }
        
                stage('CI Context') {
                    steps {
                        script {
                            def userCauses = currentBuild.getBuildCauses(
                                'hudson.model.Cause$UserIdCause'
                            )
                            def appBuildNumber = bat(
                                    script: '@git rev-list --count HEAD',
                                    returnStdout: true
                            ).trim()
                            def shortSha = bat(
                                script: '@git rev-parse --short=8 HEAD',
                                returnStdout: true
                            ).trim()
                            def fullSha = bat(
                                script: '@git rev-parse HEAD',
                                returnStdout: true
                            ).trim()
                            
                            boolean manualBuild = !userCauses.isEmpty()
                            boolean mainBranch = env.BRANCH_NAME == 'main'
                            boolean releaseBranch = env.BRANCH_NAME?.startsWith('release/')
                            boolean buildBranch = mainBranch || releaseBranch
                            
                            env.BUILD_MODE = manualBuild ? 'Manual' : 'Automatic'
                            env.EFFECTIVE_BUILD_NUMBER = appBuildNumber
                            env.EFFECTIVE_COMMIT_SHA = shortSha
                            env.EFFECTIVE_GIT_COMMIT = fullSha
                            
                            if (manualBuild && buildBranch) {
                                def version = params.VERSION.trim()
                                
                                if (!(version ==~ /^\d+\.\d+\.\d+$/)) {
                                    error(
                                        "Invalid VERSION: '${version}'. " +
                                        "Expected format: MAJOR.MINOR.PATCH (example: 1.2.3)"
                                    )
                                }
                                
                                env.EFFECTIVE_PLATFORM = params.PLATFORM
                                env.EFFECTIVE_ENVIRONMENT = params.ENVIRONMENT
                                env.EFFECTIVE_CONFIGURATION = params.CONFIGURATION
                                env.EFFECTIVE_PACKAGE_FORMAT = params.PACKAGE_FORMAT
                                env.EFFECTIVE_BUILD_VERSION = version
                            }
                            else {
                                env.EFFECTIVE_PLATFORM = 'All'
                                env.EFFECTIVE_ENVIRONMENT = 'Dev'
                                env.EFFECTIVE_CONFIGURATION = 'Development'
                                env.EFFECTIVE_PACKAGE_FORMAT = 'apk'
                                env.EFFECTIVE_BUILD_VERSION = ''
                            }
                            
                            bat '''
                                echo BUILD_MODE : %BUILD_MODE%
                                echo EFFECTIVE_BUILD_NUMBER : %EFFECTIVE_BUILD_NUMBER%
                                echo EFFECTIVE_COMMIT_SHA : %EFFECTIVE_COMMIT_SHA%
                                echo EFFECTIVE_PLATFORM : %EFFECTIVE_PLATFORM%
                                echo EFFECTIVE_ENVIRONMENT : %EFFECTIVE_ENVIRONMENT%
                                echo EFFECTIVE_CONFIGURATION : %EFFECTIVE_CONFIGURATION%
                                echo EFFECTIVE_PACKAGE_FORMAT : %EFFECTIVE_PACKAGE_FORMAT%
                                echo EFFECTIVE_BUILD_VERSION : %EFFECTIVE_BUILD_VERSION%
                            '''
                        }
                    }
                }
                
                stage('Prepare') {
                    steps {
                        bat '''
                            @echo off
                            
                            echo Reset tracked workspace files...
                            git reset --hard HEAD
                            
                            if exist "Builds\\Android" (
                                rmdir /s /q "Builds\\Android"
                            )
                            
                            if exist "%UNITY_WIN_LOG%" (
                                del /q "%UNITY_WIN_LOG%"
                            )
                            
                            if exist "%TEST_RESULTS_DIR%" (
                                rmdir /s /q "%TEST_RESULTS_DIR%"
                            )
                            
                            mkdir "%TEST_RESULTS_DIR%"
                            
                            if exist "%EDITMODE_LOG%" (
                                del /q "%EDITMODE_LOG%"
                            )
                            
                            if exist "%PLAYMODE_LOG%" (
                                del /q "%PLAYMODE_LOG%"                        
                            )
                        '''
                    }
                }
                
                stage('EditMode Tests') {
                    steps {
                        script {
                            int exitCode = bat(
                                returnStatus: true,
                                script: '''
                                    @echo off
                                    
                                    echo ========================================
                                    echo EditMode Tests
                                    echo ========================================
                                    echo Workspace : %WORKSPACE%
                                    echo ========================================
                                    
                                    "%UNITY_WIN_EXE%" ^
                                        -batchmode ^
                                        -projectPath "%WORKSPACE%" ^
                                        -runTests ^
                                        -testPlatform EditMode ^
                                        -testResults "%WORKSPACE%\\%TEST_RESULTS_DIR%\\%EDITMODE_RESULTS%" ^
                                        -logFile "%WORKSPACE%\\%EDITMODE_LOG%"
                                        
                                    set UNITY_EXIT_CODE=%ERRORLEVEL%
                                    
                                    if exist "%EDITMODE_LOG%" (
                                        type "%EDITMODE_LOG%"
                                    )
                                    
                                    exit /b %UNITY_EXIT_CODE%
                                '''
                            )
                            
                            env.EDITMODE_EXIT_CODE = exitCode.toString()
                            
                            echo "Unity Edit Test Runner exit code: ${exitCode}"
                        }
                            
                        nunit(
                            testResultsPattern: 'TestResults/editmode-results.xml',
                            failIfNoResults: true,
                            failedTestsFailBuild: false
                        )
                        
                        script {
                            if (env.EFFECTIVE_ENVIRONMENT == 'Prod' &&
                                currentBuild.currentResult != 'SUCCESS') {
        
                                error('Prod quality gate failed: EditMode tests did not pass.')                        
                            }
                            
                            if (env.EFFECTIVE_ENVIRONMENT == 'Dev' &&
                                currentBuild.currentResult == 'UNSTABLE') {
        
                                echo 'Dev quality gate: edit test failures detected, continuing build.'
                            }
                        }
                    }
                    
                    post {
                        always {
                            archiveArtifacts(
                                artifacts: 'TestResults/*.xml, editmode-tests.log',
                                allowEmptyArchive: true
                            )
                        }
                    }
                }
                
                stage('PlayMode Tests') {
                    steps {
                        script {
                            int exitCode = bat(
                                returnStatus: true,
                                script: '''
                                    @echo off
                                    
                                    echo ========================================
                                    echo PlayMode Tests
                                    echo ========================================
                                    echo Workspace : %WORKSPACE%
                                    echo ========================================
                                    
                                    "%UNITY_WIN_EXE%" ^
                                        -batchmode ^
                                        -projectPath "%WORKSPACE%" ^
                                        -runTests ^
                                        -testPlatform PlayMode ^
                                        -testResults "%WORKSPACE%\\%TEST_RESULTS_DIR%\\%PLAYMODE_RESULTS%" ^
                                        -logFile "%WORKSPACE%\\%PLAYMODE_LOG%"
                                        
                                    set UNITY_EXIT_CODE=%ERRORLEVEL%
                                    
                                    if exist "%PLAYMODE_LOG%" (
                                        type "%PLAYMODE_LOG%"
                                    )
                                    
                                    exit /b %UNITY_EXIT_CODE%
                                '''
                            )
                            
                            env.PLAYMODE_EXIT_CODE = exitCode.toString()
                            
                            echo "Unity Play Mode Test Runner exit code: ${exitCode}"
                        }
                            
                        nunit(
                            testResultsPattern: 'TestResults/playmode-results.xml',
                            failIfNoResults: true,
                            failedTestsFailBuild: false
                        )
                        
                        script {
                            if (env.EFFECTIVE_ENVIRONMENT == 'Prod' &&
                                currentBuild.currentResult != 'SUCCESS') {
        
                                error('Prod quality gate failed: PlayMode tests did not pass.')                        
                            }
                            
                            if (env.EFFECTIVE_ENVIRONMENT == 'Dev' &&
                                currentBuild.currentResult == 'UNSTABLE') {
        
                                echo 'Dev quality gate: play test failures detected, continuing build.'
                            }
                        }
                    }
                    
                    post {
                        always {
                            archiveArtifacts(
                                artifacts: 'TestResults/*.xml, playmode-tests.log',
                                allowEmptyArchive: true
                            )
                        }
                    }
                }
            }
        }
        
        stage('Build Platforms') {
            when {
                anyOf {
                    branch 'main'
                    
                    branch(
                        pattern: 'release/*',
                        comparator: 'GLOB'
                    )
                }
            }
            
            parallel {
                stage('Android') {
                    when {
                        beforeAgent true
                
                        expression {
                            env.EFFECTIVE_PLATFORM == 'All' ||
                            env.EFFECTIVE_PLATFORM == 'Android'
                        }
                    }
                    
                    agent {
                        label 'unity-win'
                    }
                    
                    steps {
                        withCredentials([
                            file(
                                credentialsId: 'android-keystore',
                                variable: 'ANDROID_KEYSTORE_PATH'
                            ),
                            string(
                                credentialsId: 'android-keystore-password',
                                variable: 'ANDROID_KEYSTORE_PASSWORD'
                            ),
                            string(
                                credentialsId: 'android-key-alias',
                                variable: 'ANDROID_KEY_ALIAS'
                            ),
                            string(
                                credentialsId: 'android-key-password',
                                variable: 'ANDROID_KEY_PASSWORD'
                            )
                        ]) {
                            bat '''
                                @echo off
                                
                                echo ========================================
                                echo Android Build
                                echo ========================================
                                echo Environment : %EFFECTIVE_ENVIRONMENT%
                                echo Configuration : %EFFECTIVE_CONFIGURATION%
                                echo Package : %EFFECTIVE_PACKAGE_FORMAT%
                                echo Build Number : %BUILD_NUMBER%
                                echo Workspace : %WORKSPACE%
                                echo ========================================
                                
                                set "BUILD_VERSION="
                                
                                if not "%EFFECTIVE_BUILD_VERSION%"=="" (
                                    set "BUILD_VERSION=-buildVersion %EFFECTIVE_BUILD_VERSION%"
                                )
                                
                                "%UNITY_WIN_EXE%" ^
                                    -batchmode ^
                                    -quit ^
                                    -projectPath "%WORKSPACE%" ^
                                    -executeMethod Build.BuildCommand.Build ^
                                    -buildTarget Android ^
                                    -target Android ^
                                    -environment %EFFECTIVE_ENVIRONMENT% ^
                                    -configuration %EFFECTIVE_CONFIGURATION% ^
                                    -buildNumber %EFFECTIVE_BUILD_NUMBER% ^
                                    -packageFormat %EFFECTIVE_PACKAGE_FORMAT% ^
                                    %BUILD_VERSION% ^
                                    -logFile "%WORKSPACE%\\%UNITY_WIN_LOG%" ^
                                    -commitSHA %EFFECTIVE_COMMIT_SHA%
                                    
                                    set UNITY_EXIT_CODE=%ERRORLEVEL%
                                    
                                    if exist "%UNITY_WIN_LOG%" (
                                        type "%UNITY_WIN_LOG%"
                                    )
                                    
                                    exit /b %UNITY_EXIT_CODE%
                            '''
                        }
                    }
                    
                    post {
                        success {
                            archiveArtifacts(
                                artifacts: 'Builds/Android/*',
                                fingerprint: true
                            )
                        }
                        
                        always {
                            archiveArtifacts(
                                artifacts: 'unity-build.log',
                                allowEmptyArchive: true
                            )
                        }
                    }
                }
                
                stage('iOS') {
                    when {
                        beforeAgent true
                
                        expression {
                            env.EFFECTIVE_PLATFORM == 'All' ||
                            env.EFFECTIVE_PLATFORM == 'iOS'
                        }
                    }
                    
                    agent {
                        label 'unity-mac'
                    }
                    
                    steps {
                        checkout scm
                        
                        sh '''#!/bin/bash

set -e
set -o pipefail

git checkout -f "$EFFECTIVE_GIT_COMMIT"
git reset --hard "$EFFECTIVE_GIT_COMMIT"

CURRENT_COMMIT=$(git rev-parse HEAD)

if [ "$CURRENT_COMMIT" != "$EFFECTIVE_GIT_COMMIT" ]; then
    echo "ERROR: Git commit mismatch."
    exit 1
fi

export DEVELOPER_DIR="$XCODE_DEVELOPER"

rm -rf "$WORKSPACE/Builds/iOS"
                        
rm -f "$WORKSPACE/$UNITY_IOS_LOG"
rm -f "$WORKSPACE/$XCODE_ARCHIVE_LOG"
rm -f "$WORKSPACE/$XCODE_EXPORT_LOG"

mkdir -p "$WORKSPACE/Builds/iOS"

VERSION_ARGS=()

if [ -n "$EFFECTIVE_BUILD_VERSION" ]; then
    VERSION_ARGS=(
        -buildVersion
        "$EFFECTIVE_BUILD_VERSION"
    )
fi

echo ""
echo "=== Unity iOS Build ==="

"$UNITY_MAC_EXE" \
    -batchmode \
    -quit \
    -projectPath "$WORKSPACE" \
    -executeMethod Build.BuildCommand.Build \
    -buildTarget iOS \
    -target iOS \
    -environment "$EFFECTIVE_ENVIRONMENT" \
    -configuration "$EFFECTIVE_CONFIGURATION" \
    -buildNumber "$EFFECTIVE_BUILD_NUMBER" \
    "${VERSION_ARGS[@]}" \
    -commitSHA "$EFFECTIVE_COMMIT_SHA" \
    -logFile "$WORKSPACE/$UNITY_IOS_LOG"

ENV_LOWER=$(
    echo "$EFFECTIVE_ENVIRONMENT" |
    tr '[:upper:]' '[:lower:]'
)

CONFIG_LOWER=$(
    echo "$EFFECTIVE_CONFIGURATION" |
    tr '[:upper:]' '[:lower:]'
)

echo ""
echo "=== Xcode Path Find ==="

XCODE_PROJECT=$(
    find "$WORKSPACE/Builds/iOS" \
        -type d \
        -name "Unity-iPhone.xcodeproj" \
        -print \
        -quit
)

if [ -z "$XCODE_PROJECT" ]; then
    echo "ERROR: Xcode project was not created."

    echo ""
    echo "Builds/iOS contents:"
    find "$WORKSPACE/Builds/iOS" -maxdepth 2 -print

    exit 1
fi

IOS_PROJECT_DIR=$(dirname "$XCODE_PROJECT")

echo ""
echo "Xcode Project:"
echo "$XCODE_PROJECT"

echo "iOS Project Directory:"
echo "$IOS_PROJECT_DIR"

echo "Xcode Project:"
echo "$IOS_PROJECT_DIR"

if [ ! -d "$IOS_PROJECT_DIR/Unity-iPhone.xcodeproj" ]; then
    echo "ERROR: Xcode project was not created."
    exit 1
fi

echo ""
echo "=== Xcode Archive ==="

ARCHIVE_DIR="$WORKSPACE/Builds/iOS/Archive"
ARCHIVE_PATH="$ARCHIVE_DIR/UnityJenkinsCICD.xcarchive"

mkdir -p "$ARCHIVE_DIR"

xcodebuild \
    -project "$IOS_PROJECT_DIR/Unity-iPhone.xcodeproj" \
    -scheme Unity-iPhone \
    -configuration Release \
    -destination "generic/platform=iOS" \
    -archivePath "$ARCHIVE_PATH" \
    -allowProvisioningUpdates \
    archive \
    2>&1 \
    | tee "$WORKSPACE/$XCODE_ARCHIVE_LOG"


if [ ! -d "$ARCHIVE_PATH" ]; then
    echo "ERROR: xcarchive was not created."
    exit 1
fi

echo ""
echo "=== IPA Export ==="

EXPORT_OPTIONS="$WORKSPACE/Builds/iOS/ExportOptions.plist"
                        
cat > "$EXPORT_OPTIONS" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
"http://www.apple.com/DTDs/PropertyList-1.0.dtd">

<plist version="1.0">
<dict>
    <key>method</key>
    <string>debugging</string>

    <key>teamID</key>
    <string>383CQM387M</string>

    <key>signingStyle</key>
    <string>automatic</string>
</dict>
</plist>
EOF
    
EXPORT_DIR="$WORKSPACE/Builds/iOS/Export"
                        
mkdir -p "$EXPORT_DIR"

xcodebuild \
    -exportArchive \
    -archivePath "$ARCHIVE_PATH" \
    -exportPath "$EXPORT_DIR" \
    -exportOptionsPlist "$EXPORT_OPTIONS" \
    -allowProvisioningUpdates \
    2>&1 \
    | tee "$WORKSPACE/$XCODE_EXPORT_LOG"

IPA_PATH=$(
    find "$EXPORT_DIR" \
        -maxdepth 1 \
        -type f \
        -name "*.ipa" \
        -print \
        -quit
)


if [ -z "$IPA_PATH" ]; then
    echo "ERROR: IPA was not created."
    exit 1
fi

APP_VERSION=$(
    /usr/libexec/PlistBuddy \
        -c "Print :ApplicationProperties:CFBundleShortVersionString" \
        "$ARCHIVE_PATH/Info.plist"
)

FINAL_NAME="UnityJenkinsCICD_${ENV_LOWER}_${CONFIG_LOWER}_${APP_VERSION}_${EFFECTIVE_BUILD_NUMBER}_${EFFECTIVE_COMMIT_SHA}"
FINAL_IPA="$EXPORT_DIR/${FINAL_NAME}.ipa"

if [ "$IPA_PATH" != "$FINAL_IPA" ]; then
    mv "$IPA_PATH" "$FINAL_IPA"
fi

FINAL_ARCHIVE="$ARCHIVE_DIR/${FINAL_NAME}.xcarchive.zip"
                        
ditto \
    -c \
    -k \
    --keepParent \
    "$ARCHIVE_PATH" \
    "$FINAL_ARCHIVE"

echo ""
echo "Final Name: $FINAL_NAME"
echo "Final Ipa: $FINAL_IPA"
echo "Final Archive: $FINAL_ARCHIVE"
'''
                    }
                    
                    post {
                        success {
                            archiveArtifacts(
                                artifacts:
                                    'Builds/iOS/Export/*.ipa, Builds/iOS/Archive/*.xcarchive.zip',
                                fingerprint: true
                            )
                        }

                        always {
                            archiveArtifacts(
                                artifacts:
                                    'unity-ios-build.log, xcode-archive.log, xcode-export.log',
                                allowEmptyArchive: true
                            )
                        }
                    }
                }
            }
        }
    }
    
    post {
        success {
            echo 'Pipeline succeeded.'
        }
        
        unstable {
            echo 'Pipeline complated, but quality checks reported problems.'
        }
        
        failure {
            echo 'Pipeline failed.'
        }
        
        aborted {
            echo 'Pipeline was aborted.'
        }
        
        always {
            echo "Finished Jenkins Build #${env.BUILD_NUMBER}"
        }
    }
}