# UnityJenkinsCICD
## 개요
Unity 프로젝트의 테스트와 Android / iOS 빌드를 자동화 하기 위해 구축한 Jenkins 기반 CI/CD 샘플 프로젝트입니다.

Windows와 macOS 환경에 Jenkins Agent를 구성하고,
GitHub와 연동하여 코드 변경에 따른 유닛 자동화 테스트 및 브랜치에 따른 플랫폼별 빌드가 이루어지도록 구현했습니다.

단순히 빌드를 실행하는 것 뿐 아니라, 개발 환경에 따른 빌드 정책, 테스트 결과 검증, 버전 관리, 빌드 관리등을 자동화 하는 것을 목적으로 합니다.

현재는 자동 테스트, 빌드 서명 및 빌드 보관까지 구현되어 있으며, Google Play / App Store로의 자동 배포는 포함되어있지 않습니다.

## 개발 환경
- Unity 6.3
- Jenkins Multibranch Pipeline (Windowns, Mac OS)
- Github Webhook

## CI/CD 아키텍처
<img width="1920" height="865" alt="ScreenShot Tool -20261008225209" src="https://github.com/user-attachments/assets/785d22c9-9c24-4b11-a3e5-5493e44a27c2" />

이 프로젝트는 Windows 환경에서 젠킨스 컨트롤러를 실행하고, 플랫폼 별 Windows, Mac OS에서 동작하는 agent를 사용하는 구조로 되어 있습니다.

Windows에서는 유닛 테스트, Android 빌드를 담당하고, macOS는 iOS 빌드 생성과 XCode 프로젝트의 xarchive, ipa 생성까지 담당합니다.

Jenkins Pipeline은 테스트 단계와 빌드 단계를 분리하여 구성했습니다.

## 자동 빌드 및 수동 빌드
<img width="600" height="845" alt="mermaid-diagram (1)" src="https://github.com/user-attachments/assets/36972f31-bd1f-41bb-99a7-9516eb065ed9" />

Github Webhook을 통해 자동으로 실행되는 빌드와 Jenkins에서 파라미터를 지정하여 실행하는 수동 빌드를 지원합니다.

### 자동 빌드
자동 빌드는 Dev 환경의 Development 빌드를 기준으로 수행합니다.
- main 브랜치: 유닛 테스트 및 Android / iOS 빌드 자동화
- PR생성 및 갱신: 유닛 테스트 자동화

### 수동 빌드
Jenkins 파라미터를 통해 수동 빌드를 할 수 있습니다.
수동 빌드는 main, release/* 브랜치에서만 진행되도록 제한되어 있습니다.

- Platform: All / Android / iOS
- Environment: Dev / Prod
- Configuration: Development / Release
- Version: Major.Minor.Patch

## 유닛 테스트
빌드 전에 EditMode / PlayMode 유닛 테스트를 자동 실행합니다.

진행 환경에 따라 테스트 실패 시 정책을 다르게 적용했습니다.
- Environment Dev: Unstable로 기록하고 젠킨스 빌드는 계속 진행
- Environment Prod: 실패 시, 전체 파이프라인을 중단

## 프로젝트 내용
프로젝트 내에 인게임 컨텐츠 및 유닛 테스트 코드는 Codex Agent를 사용하여 생성되었습니다.
