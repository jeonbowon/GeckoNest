/// <summary>효과음 이름. 파일로 교체할 때는 Resources/Audio/Sfx/ 에 이 이름(소문자)으로 넣는다. 예: tap.wav, moltsuccess.ogg</summary>
public enum Sfx
{
    Tap,          // 버튼 누름
    Pop,          // 작은 등장·선택
    Lick,         // 혀 '쪽'
    Crunch,       // 먹이 오물오물
    Spray,        // 분무 (물 주기)
    Drip,         // 물방울
    Heart,        // 쓰다듬기 좋아함
    Sparkle,      // 반짝 (청소)
    Chime,        // 성장
    MoltSuccess,  // 허물 성공
    MoltFail,     // 허물 실패
    Coin,         // 재화 획득
    Refuse,       // 거절 (배불러요)
    Annoyed,      // 삐짐 (그만 만져요)
    Boing,        // 점프
    Error,        // 구매 실패 등
}
