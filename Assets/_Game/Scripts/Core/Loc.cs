using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage { Korean, English }

/// <summary>
/// 화면 문구 번역 (한국어 / 영어).
///
/// 언어: 설정(SettingsData.language)이 "ko"·"en"이면 그것, 비어 있으면 기기 언어 — 한국어 기기면 한국어, 그 밖에는 영어.
/// 코드:   Loc.Get("home.feed") · Loc.Format("reward.streak", 3) · Loc.Pick("line.pet") (| 로 나눈 여러 문구 중 하나)
/// 씬 글자: 번역표에 있는 원문(한글이든 영어든)과 똑같으면 씬이 열릴 때 현재 언어로 바뀐다 (SceneTextLocalizer).
///
/// 새 문구는 반드시 아래 표에 한국어·영어를 함께 넣는다. 글꼴에 없는 기호(★ ← … 이모지)는 쓰지 않는다.
/// 한국어와 영어의 {0} 자리 수는 같아야 한다 — 자가 검사가 확인한다.
/// </summary>
public static class Loc
{
    private static readonly Dictionary<string, (string ko, string en)> s_table = new Dictionary<string, (string ko, string en)>
    {
        // ── 공통 ──────────────────────────────────────────────
        ["common.back"]                 = ("< 홈으로", "< Back"),
        ["common.cancel"]               = ("취소", "Cancel"),
        ["common.free"]                 = ("무료", "Free"),
        ["common.owned"]                = ("보유", "Owned"),
        ["common.remove"]               = ("빼기", "Remove"),
        ["common.not_found"]            = ("항목을 찾을 수 없습니다.", "Item not found."),
        ["common.need_coin"]            = ("코인이 부족합니다. (필요: {0})", "Not enough coins. (Need {0})"),
        ["common.need_gem"]             = ("젬이 부족합니다. (필요: {0})", "Not enough gems. (Need {0})"),
        ["common.need_coin_have"]       = ("코인이 부족합니다. (필요: {0}, 보유: {1})", "Not enough coins. (Need {0}, have {1})"),
        ["common.need_gem_have"]        = ("젬이 부족합니다. (필요: {0}, 보유: {1})", "Not enough gems. (Need {0}, have {1})"),

        // ── 하단 탭 ───────────────────────────────────────────
        ["nav.store"]                   = ("상점", "Store"),
        ["nav.gecko"]                   = ("게코", "Geckos"),
        ["nav.decor"]                   = ("꾸미기", "Decor"),
        ["nav.reward"]                  = ("보상", "Rewards"),
        ["nav.settings"]                = ("설정", "Settings"),

        // ── 윗줄 재화 ({0} = 천 단위 쉼표를 넣은 숫자) ─────────
        ["hud.coin"]                    = ("코인 {0}", "Coins {0}"),
        ["hud.gem"]                     = ("젬 {0}", "Gems {0}"),

        // ── 홈: 돌봄 버튼 · 상태 ──────────────────────────────
        ["home.feed"]                   = ("먹이", "Feed"),
        ["home.water"]                  = ("물", "Water"),
        ["home.pet"]                    = ("쓰다듬기", "Pet"),
        ["home.clean"]                  = ("청소", "Clean"),
        ["home.no_food"]                = ("먹이 없음", "No food"),

        // ── 먹이 효과 (선반 칸 · 먹은 뒤 말풍선) ──────────────
        ["food.hunger"]                 = ("배고픔 +{0}", "Hunger +{0}"),
        ["food.growth"]                 = ("성장 +{0}", "Growth +{0}"),
        ["food.mood"]                   = ("기분 +{0}", "Mood +{0}"),
        ["food.health"]                 = ("건강 +{0}", "Health +{0}"),
        ["food.molt"]                   = ("허물 +{0}%", "Molt +{0}%"),
        ["food.favorite"]               = ("좋아함", "Favorite"),
        ["line.favorite"]               = ("최고야!|이거 좋아!|너무 맛있어!", "The best!|I love this!|So tasty!"),
        ["food.useless"]                = ("필요 없음", "Not needed"),
        ["line.grown"]                  = ("다 자라서 필요 없어요|이제 안 먹어도 돼요", "I'm all grown up|I don't need that now"),
        ["line.poke"]                   = ("앗!|깜짝이야!", "Oh!|You startled me!"),
        ["line.tail"]                   = ("꼬리는 안 돼!|꼬리 만지지 마~", "Not my tail!|Hands off my tail~"),

        // ── 게코 부위별 반응 (| 로 나눈 것 중 하나) ─────────────
        ["line.eye_wipe"]               = ("눈 닦는 중~|반짝반짝 닦아야지", "Cleaning my eyes~|Squeaky clean!"),
        ["line.eye_no"]                 = ("눈은 안 돼!|앗, 눈이야!", "Not my eyes!|Ow, my eye!"),
        ["line.mouth"]                  = ("냠?|뭐 먹을 거야?", "Yum?|Is that food?"),
        ["line.mouth_hungry"]           = ("배고파요!|먹을 거 주세요!", "I'm hungry!|Food, please!"),
        ["line.yawn"]                   = ("하아암~|졸려...", "Yawn~|So sleepy..."),
        ["line.wave"]                   = ("안녕!|반가워!", "Hi!|Hello there!"),
        ["line.paw"]                    = ("발 만지지 마~|간질간질", "Not my paw~|That tickles"),
        ["line.giggle"]                 = ("간지러워!|히히", "Ticklish!|Tee-hee"),
        ["line.kick"]                   = ("뒷발 차기!|저리 가~", "Back kick!|Go away~"),
        ["line.shiver"]                 = ("부르르|오싹해~", "Brrr|Shivers~"),
        ["line.tail_base"]              = ("꼬리 건드리지 마|흥!", "Leave my tail alone|Hmph!"),
        ["line.sleepy"]                 = ("음냐...|졸려요...", "Mmm...|Too sleepy..."),
        ["line.grumpy"]                 = ("건드리지 마!|지금 기분 별로야", "Don't touch me!|Not in the mood"),
        ["line.climb"]                  = ("깜짝이야!|여기가 좋아~", "Whoa!|I like it up here~"),
        ["line.peek"]                   = ("누구야?|나 여기 있어!", "Who's there?|Here I am!"),
        ["line.perch"]                  = ("여기 좋다~|높은 데 최고", "Nice up here~|I love high places"),
        // 장식 찾아가기 (2026-09-21) — 이끼 바위 비비기 · 화분 물방울 · 바위 몸 데우기
        ["line.visit.rub"]              = ("근질근질... 시원해~|바위에 쓱쓱", "So itchy... ahh~|Scritch scritch"),
        ["line.visit.rub_idle"]         = ("지금은 안 가려워|킁킁", "Not itchy right now|Sniff sniff"),
        ["line.visit.drink"]            = ("물방울 맛있다!|할짝할짝", "Tasty droplets!|Lick lick"),
        ["line.visit.bask"]             = ("따뜻하다~|바위가 따끈따끈", "So warm~|Toasty rock"),
        // 꾸미기 카드의 효과 한 줄 (DecorPerks)
        ["perk.moltrub"]                = ("허물 성공 +{0}%", "Shedding +{0}%"),
        ["perk.droplets"]               = ("물 +{0}", "Water +{0}"),
        ["perk.basking"]                = ("건강 회복 +{0}%", "Healing +{0}%"),
        ["perk.shelter"]                = ("기분 감소 -{0}%", "Mood loss -{0}%"),
        ["perk.play"]                   = ("쓰다듬기 애정 +{0}", "Pet bond +{0}"),

        // ── 꾸미기 구조물 ─────────────────────────────────────
        ["terrarium.floor_full"]        = ("바닥 자리가 가득 찼어요\n놓은 장식을 눌러 빼 주세요", "Floor spots are full\nTap a placed item to remove it"),
        ["terrarium.wall_full"]         = ("벽 자리가 가득 찼어요\n놓은 장식을 눌러 빼 주세요", "Wall spots are full\nTap a placed item to remove it"),
        ["terrarium.edit_hint"]         = ("장식을 끌어서 옮기세요\n빈 곳을 누르면 끝나요", "Drag decorations to move them\nTap empty space to finish"),
        ["terrarium.edit_saved"]        = ("배치를 저장했어요", "Layout saved"),
        ["decor.decor_cork"]            =("코르크 뒤판", "Cork Backdrop"),
        ["decor.decor_vine"]            = ("덩굴", "Vine"),
        ["decor.decor_branch"]          = ("나뭇가지", "Branch"),

        // ── 오늘의 돌봄 목표 ({0} = 지금, {1} = 목표) ──────────
        ["goal.title"]                  = ("오늘의 돌봄", "Today's Care"),
        ["goal.feed"]                   = ("먹이 {0}/{1}", "Feed {0}/{1}"),
        ["goal.water"]                  = ("물 {0}/{1}", "Water {0}/{1}"),
        ["goal.pet"]                    = ("쓰다듬기 {0}/{1}", "Pet {0}/{1}"),
        ["goal.clean"]                  = ("청소 {0}/{1}", "Clean {0}/{1}"),
        ["goal.claim"]                  = ("받기  코인 +{0}", "Claim  Coins +{0}"),
        ["goal.locked"]                 = ("모두 채우면 코인 +{0}", "Finish all for Coins +{0}"),
        ["goal.claimed"]                = ("오늘은 받았어요", "Claimed today"),
        ["goal.done"]                   = ("오늘의 돌봄: {0} 완료", "Today's care: {0} done"),
        ["goal.all"]                    = ("오늘의 돌봄을 모두 채웠어요!\n보상 탭에서 받으세요", "All of today's care is done!\nClaim it in Rewards"),

        ["line.new_friend"]             =("새 친구도 키워 볼까요?", "How about raising a new friend?"),
        ["stat.hunger"]                 = ("배고픔", "Hunger"),
        ["stat.thirst"]                 = ("목마름", "Thirst"),
        ["stat.mood"]                   = ("기분", "Mood"),
        ["stat.health"]                 = ("건강", "Health"),
        ["stat.clean"]                  = ("청결", "Cleanliness"),

        // ── 성장 단계 ─────────────────────────────────────────
        ["stage.0"]                     = ("해츨링", "Hatchling"),
        ["stage.1"]                     = ("베이비", "Baby"),
        ["stage.2"]                     = ("주버나일", "Juvenile"),
        ["stage.3"]                     = ("서브어덜트", "Sub-Adult"),
        ["stage.4"]                     = ("어덜트", "Adult"),

        // ── 다음 성장 조건 (성장 단계 글자를 누르면 말풍선) ─────
        ["growth.next"]                 = ("다음 성장: {0}", "Next: {0}"),
        ["growth.adult"]                = ("다 자랐어요!", "Fully grown!"),
        ["growth.req_age"]              = ("나이 {0}일", "Age {0} days"),
        ["growth.req_molt"]             = ("허물 {0}회", "Molts {0}"),
        ["growth.req_health"]           = ("건강 {0}", "Health {0}"),
        ["growth.req_affection"]        = ("애정도 {0}", "Affection {0}"),
        ["growth.met"]                  = ("{0} - 충족", "{0} - OK"),
        ["growth.unmet"]                = ("{0} - 부족 (지금 {1})", "{0} - Need (now {1})"),

        // ── 첫 실행 부화 연출 ({0} = 이름+조사) ────────────────
        ["hatch.hint"]                  = ("톡톡 두드려 주세요", "Tap the egg!"),
        ["hatch.hello"]                 = ("안녕! 반가워!", "Hi! Nice to meet you!"),
        ["hatch.born"]                  = ("{0} 태어났어요!", "{0} hatched!"),

        // ── 게코 한마디 (| 로 나눈 것 중 하나를 고른다) ────────
        ["line.full"]                   = ("배불러요|이미 배불러~", "I'm full|Too full~"),
        ["line.not_thirsty"]            = ("목 안 말라요|물은 충분해!", "Not thirsty|I had enough water!"),
        ["line.clean"]                  = ("이미 반짝반짝!|깨끗해요~", "Already sparkling!|All clean~"),
        ["line.annoyed"]                = ("그만 만져~|힝, 귀찮아|잠깐만 쉴래", "Stop it~|Hmph, enough|Let me rest"),
        ["line.pet"]                    = ("좋아~|헤헤|더 해줘!", "Nice~|Hehe|More please!"),
        ["line.fed"]                    = ("냠냠|맛있다!", "Yum yum|Tasty!"),
        ["line.growth"]                 = ("쑥쑥!|나 컸지?", "Growing!|Look, I grew!"),
        ["line.molt"]                   = ("개운해!|새 옷 입었다!", "So fresh!|New skin!"),

        // ── 성장 · 허물 결과 알림 ({0} = 이름+조사) ────────────
        ["event.growth"]                = ("{0} 자랐어요!\n{1} -> {2}", "{0} grew up!\n{1} -> {2}"),
        ["event.molt_success"]          = ("{0} 허물을 벗었어요!\n({1}번째 허물)", "{0} shed its skin!\n(Molt #{1})"),
        ["event.adult"]                 = ("{0} 다 자랐어요!\n코인 +{1}  젬 +{2}", "{0} is all grown up!\nCoins +{1}  Gems +{2}"),
        ["event.adult_coin"]            = ("{0} 다 자랐어요!\n코인 +{1}", "{0} is all grown up!\nCoins +{1}"),
        ["geckolist.grown"]             = ("{0} - 다 자람", "{0} - Grown"),
        ["event.molt_fail"]             =("허물이 잘 안 벗겨졌어요\n다음엔 꼭 성공할 거예요", "The shed didn't come off\nIt will work next time"),

        // ── 일일 보상 ─────────────────────────────────────────
        ["reward.title"]                = ("일일 보상", "Daily Reward"),
        ["reward.streak"]               = ("연속 {0}일", "Day {0} streak"),
        ["reward.coin"]                 = ("코인 +{0}", "Coins +{0}"),
        ["reward.coin_gem"]             = ("코인 +{0}  젬 +{1}", "Coins +{0}  Gems +{1}"),
        ["reward.claim"]                = ("받기", "Claim"),
        ["reward.tomorrow"]             = ("내일 다시", "Come back tomorrow"),
        ["reward.got_coin"]             = ("코인 +{0} 수령!", "Got {0} coins!"),
        ["reward.got_coin_gem"]         = ("코인 +{0}  젬 +{1} 수령!", "Got {0} coins and {1} gems!"),
        ["reward.claimed"]              = ("수령 완료!", "Claimed!"),
        ["reward.reset"]                = ("매일 {0}에 새로 고침", "Resets daily at {0}"),

        // ── 설정 ──────────────────────────────────────────────
        ["settings.bgm"]                = ("배경음", "BGM"),
        ["settings.sfx"]                = ("효과음", "Sound FX"),
        ["settings.vibration"]          = ("진동", "Vibration"),
        ["settings.notification"]       = ("알림", "Notifications"),
        ["settings.privacy"]            = ("개인정보 처리방침", "Privacy Policy"),

        // ── 상점 · 게코 목록 · 꾸미기 ─────────────────────────
        ["store.buy"]                   = ("구매", "Buy"),
        ["geckolist.title"]             = ("내 게코", "My Geckos"),
        ["geckolist.adopt_open"]        = ("+ 분양", "+ Adopt"),
        ["geckolist.adopt_title"]       = ("게코 분양", "Adopt a Gecko"),
        ["geckolist.adopt_confirm"]     = ("분양받기", "Adopt!"),
        ["geckolist.name_placeholder"]  = ("이름을 입력하세요", "Gecko name..."),
        ["geckolist.option_free"]       = ("{0}  (무료)", "{0}  (Free)"),
        ["geckolist.full"]              = ("게코는 최대 {0}마리까지 키울 수 있어요", "You can keep up to {0} geckos"),
        ["geckolist.here"]              = ("홈에 있어요", "At home"),
        ["geckolist.status_ok"]         = ("잘 지내요", "Doing well"),
        ["geckolist.status_sick"]       = ("아파요", "Unwell"),
        ["geckolist.status_hungry"]     = ("배고파요", "Hungry"),
        ["geckolist.status_thirsty"]    = ("목말라요", "Thirsty"),
        ["geckolist.status_dirty"]      = ("청소 필요", "Needs cleaning"),
        ["geckolist.status_gift"]       = ("선물이 있어요", "Gift waiting"),
        ["gift.coin"]                   = ("{0}의 선물!\n코인 +{1}", "A gift from {0}!\nCoins +{1}"),
        ["gift.coin_food"]              = ("{0}의 선물!\n코인 +{1}  {2} +1", "A gift from {0}!\nCoins +{1}  {2} +1"),
        ["line.gift"]                   = ("선물이야!|찾았구나~|너 주려고 모았어", "A present for you!|You found it!|Saved this for you"),
        ["terrarium.locked"]            = ("어덜트 {0}마리", "{0} adults"),
        ["terrarium.locked_hint"]       = ("어덜트를 {0}마리 키우면 열려요", "Raise {0} adult geckos to unlock"),
        ["terrarium.unlocked"]          = ("새 장식이 열렸어요!\n{0}", "New decor unlocked!\n{0}"),
        // ── 도감 · 업적 (게코 목록 > 도감 창, CollectionPanel) ──
        ["book.button"]                 = ("도감 / 업적", "Book"),
        ["book.button_count"]           = ("도감 / 업적 ({0})", "Book ({0})"),
        ["book.title"]                  = ("게코 도감", "Gecko Book"),
        ["book.close"]                  = ("닫기", "Close"),
        ["book.tab_book"]               = ("도감", "Collection"),
        ["book.tab_achieve"]            = ("업적", "Achievements"),
        ["book.unknown"]                = ("???", "???"),
        ["book.stamp_met"]              = ("만남", "Met"),
        ["book.complete"]               = ("모든 종을 어덜트로 키우기 ({0}/{1})", "Raise every species to adult ({0}/{1})"),
        ["book.claimed"]                = ("받았어요", "Claimed"),
        ["book.met_reward"]             = ("도감에 새 친구를 기록했어요!\n코인 +{0}", "New friend added to your book!\nCoins +{0}"),
        ["achieve.claim"]               = ("받기\n{0}", "Claim\n{0}"),
        ["achieve.reward_coin"]         = ("코인 +{0}", "Coins +{0}"),
        ["achieve.reward_gem"]          = ("젬 +{0}", "Gems +{0}"),
        ["achieve.progress"]            = ("({0}/{1})", "({0}/{1})"),
        ["achieve.done"]                = ("업적 달성! {0}\n게코 탭 > 도감에서 받아요", "Achievement unlocked: {0}\nClaim it in Geckos > Book"),
        ["achieve.first_molt"]          = ("첫 허물", "First Shed"),
        ["achieve.molt_master"]         = ("허물 달인", "Shed Master"),
        ["achieve.first_adult"]         = ("첫 어덜트", "First Adult"),
        ["achieve.gecko_family"]        = ("게코 가족", "Gecko Family"),
        ["achieve.gentle_hand"]         = ("다정한 손길", "Gentle Hands"),
        ["achieve.good_meal"]           = ("든든한 식사", "Hearty Meals"),
        ["achieve.steady_care"]         = ("꾸준한 돌봄", "Steady Care"),
        ["achieve.full_house"]          = ("북적이는 집", "Full House"),
        ["achieve.desc.molts"]          = ("허물 {0}번 벗기", "Shed {0} time(s)"),
        ["achieve.desc.adults"]         = ("어덜트 {0}마리 키우기", "Raise {0} adult(s)"),
        ["achieve.desc.pets"]           = ("쓰다듬기 {0}번", "Pet {0} times"),
        ["achieve.desc.feeds"]          = ("먹이 {0}번 주기", "Feed {0} times"),
        ["achieve.desc.goaldays"]       = ("오늘의 돌봄 {0}번 완료", "Finish daily care {0} times"),
        ["achieve.desc.geckos"]         = ("게코 {0}마리 함께 키우기", "Keep {0} geckos together"),

        ["achieve.best_friend"]         = ("단짝", "Best Friends"),
        ["achieve.desc.bondlevel"]      = ("유대 Lv.{0} 게코 키우기", "Reach Bond Lv.{0} with a gecko"),

        // ── 유대 레벨 (GeckoBond) ──────────────────────────────
        ["bond.label"]                  = ("유대 {0}", "Bond {0}"),
        ["bond.info_title"]             = ("유대 Lv.{0}", "Bond Lv.{0}"),
        ["bond.info_next"]              = ("다음 Lv.{0} ({1}/{2})", "Next Lv.{0} ({1}/{2})"),
        ["bond.info_max"]               = ("최고 레벨이에요!", "Max level!"),
        ["bond.info_perks"]             = ("풀린 것: {0}", "Unlocked: {0}"),
        ["bond.info_none"]              = ("더 돌봐 주면 친해져요", "Keep caring to grow closer"),
        ["bond.info_today_full"]        = ("오늘은 충분히 친해졌어요", "That's enough bonding for today"),
        ["bond.perk.1"]                 = ("인사", "Greeting"),
        ["bond.perk.2"]                 = ("쓰다듬기 좋아함", "Loves petting"),   // "쓰다듬기"는 버튼 문구와 겹친다
        ["bond.perk.3"]                 = ("부르기", "Calling"),
        ["bond.perk.4"]                 = ("재롱", "Tricks"),
        ["bond.perk.5"]                 = ("손바닥", "Palm ride"),
        ["bond.perk_desc.1"]            = ("홈에 오면 반갑게 인사해요", "Greets you when you come home"),
        ["bond.perk_desc.2"]            = ("쓰다듬기를 더 좋아해요", "Loves being petted even more"),
        ["bond.perk_desc.3"]            = ("빈 바닥을 두 번 톡톡 하면 다가와요", "Double-tap the floor to call it over"),
        ["bond.perk_desc.4"]            = ("쓰다듬으면 가끔 재롱을 부려요", "Sometimes shows off tricks when petted"),
        ["bond.perk_desc.5"]            = ("길게 누르면 손바닥에 올라와요", "Long-press to let it ride your palm"),
        ["event.bond"]                  = ("{0} 마음을 열었어요! 유대 Lv.{1}\n{2}  {3}", "{0} warmed up to you! Bond Lv.{1}\n{2}  {3}"),
        ["geckolist.bond"]              = ("{0}  유대 {1}", "{0}  Bond {1}"),
        ["line.bond"]                   = ("우리 친구지?|좋아!|헤헤", "We're friends, right?|Yay!|Hehe"),
        ["line.greet"]                  = ("왔구나!|기다렸어~|반가워!", "You're back!|I missed you~|Hi there!"),
        ["line.come"]                   = ("나 불렀어?|왔어!", "You called?|Here I am!"),
        ["line.trick"]                  = ("봐봐!|빙글~", "Watch this!|Wheee~"),
        ["line.palm"]                   = ("따뜻해~|높다!|손 위 최고", "So warm~|So high!|Best seat ever"),

        // ── 모프 (GeckoMorph) ─────────────────────────────────
        ["event.morph"]                 = ("{0}의 무늬가 드러났어요!\n{1} ({2})  {3}", "{0}'s pattern is revealed!\n{1} ({2})  {3}"),
        ["morph.new"]                   = ("새 모프! {0}", "New morph! {0}"),
        ["morph.rarity.0"]              = ("흔함", "Common"),
        ["morph.rarity.1"]              = ("희귀", "Rare"),
        ["morph.rarity.2"]              = ("아주 희귀", "Very rare"),
        ["morph.crested_normal"]        = ("노멀", "Normal"),
        ["morph.crested_harlequin"]     = ("할리퀸", "Harlequin"),
        ["morph.crested_red"]           = ("레드", "Red"),
        ["morph.crested_dalmatian"]     = ("달마시안", "Dalmatian"),
        ["morph.leopard_normal"]        = ("노멀", "Normal"),
        ["morph.leopard_tangerine"]     = ("탠저린", "Tangerine"),
        ["morph.leopard_albino"]        = ("알비노", "Albino"),
        ["morph.leopard_blizzard"]      = ("블리자드", "Blizzard"),
        ["morph.gargoyle_normal"]       = ("노멀", "Normal"),
        ["morph.gargoyle_red"]          = ("레드 스트라이프", "Red Stripe"),
        ["morph.gargoyle_orange"]       = ("오렌지 얼룩", "Orange Blotched"),
        ["morph.gargoyle_white"]        = ("화이트", "White"),
        ["line.morph"]                  = ("멋지지?|새 옷 입었어!|어때?", "Looking good?|New look!|How do I look?"),
        ["book.morphs"]                 = ("모프 {0}/{1}", "Morphs {0}/{1}"),
        ["geckolist.morph"]             = ("{0} - {1}", "{0} - {1}"),
        ["achieve.morph_collector"]     = ("모프 수집가", "Morph Collector"),
        ["achieve.desc.morphs"]         = ("모프 {0}종 모으기", "Collect {0} morphs"),

        ["decor.decor_moss_rock"]       = ("이끼 바위", "Mossy Rock"),
        ["decor.decor_cave"]            = ("동굴", "Cave"),
        ["decor.decor_driftwood"]       = ("큰 유목", "Driftwood"),
        ["terrarium.background"]        = ("배경", "Background"),
        ["terrarium.floor"]             = ("바닥", "Floor"),          // 씬 탭 글자 — 탭은 숨긴다 (2026-09-21)
        ["terrarium.theme"]             = ("테마", "Themes"),
        ["terrarium.decor"]             = ("장식", "Decorations"),
        ["terrarium.error"]             = ("오류 메시지", "Error"),
        ["terrarium.slots_full"]        = ("장식 슬롯이 가득 찼습니다. 놓은 장식을 다시 눌러 빼 주세요.", "Decor slots are full. Tap a placed decoration to remove it."),

        // ── 알림 ({0} = 이름+조사) ─────────────────────────────
        ["notify.channel_name"]         = ("하코 돌봄 알림", "Hako care reminders"),
        ["notify.channel_desc"]         = ("게코가 배고프거나 일일 보상이 준비되면 알려드립니다", "Lets you know when your gecko needs care or a daily reward is ready"),
        ["notify.hungry"]               = ("{0} 배고파해요", "{0} is hungry"),
        ["notify.thirsty"]              = ("{0} 목말라해요", "{0} is thirsty"),
        ["notify.check"]                = ("잠깐 들여다봐 주세요", "Come take a look"),
        ["notify.reward_title"]         = ("오늘의 보상이 기다려요", "Your daily reward is waiting"),
        ["notify.reward_text"]          = ("{0} 기다리고 있어요", "{0} is waiting for you"),

        // ── 게코 · 아이템 · 장식 이름 (키 = 종류.id, 없으면 에셋의 displayName) ──
        ["gecko.default_name"]          = ("하코", "Hako"),
        ["species.crested"]             = ("크레스티드 게코", "Crested Gecko"),
        ["species.gargoyle"]            = ("가고일 게코", "Gargoyle Gecko"),
        ["species.leopard"]             = ("레오파드 게코", "Leopard Gecko"),
        ["item.cricket_small"]          = ("귀뚜라미", "Cricket"),
        ["item.gutloaded_cricket"]      = ("것로딩 귀뚜라미", "Gut-loaded Cricket"),
        ["item.mealworm"]               = ("밀웜", "Mealworm"),
        ["item.dubia_roach"]            = ("두비아 바퀴", "Dubia Roach"),
        ["item.superworm"]              = ("슈퍼밀웜", "Superworm"),
        ["item.calcium_dusting"]        = ("칼슘+비타민 더스팅", "Calcium + Vitamin Dust"),
        ["item.growth_booster"]         = ("성장촉진제", "Growth Booster"),
        ["decor.bg_jungle"]             = ("정글 테마", "Jungle"),
        ["decor.bg_desert"]             = ("사막 테마", "Desert"),
        ["decor.floor_soil"]            = ("흙 바닥", "Soil Floor"),
        ["decor.floor_bark"]            = ("나무판 바닥", "Bark Floor"),
        ["decor.decor_rock"]            = ("바위", "Rock"),
        ["decor.decor_plant"]           = ("화분", "Potted Plant"),
        ["decor.decor_hide"]            = ("하이드 하우스", "Hide House"),
    };

    private static Dictionary<string, string> s_sourceIndex;   // 씬 원문 → 키
    private static bool s_initialized;
    private static GameLanguage s_current;

    // ── 언어 ──────────────────────────────────────────────────

    public static GameLanguage Current
    {
        get
        {
            if (!s_initialized) Set(FromDevice());
            return s_current;
        }
    }

    public static bool IsKorean => Current == GameLanguage.Korean;

    /// <summary>앱 시작 시 AppBootstrap이 설정값으로 부른다. "ko" · "en" · 빈 값(기기 언어)</summary>
    public static void Init(string setting) => Set(Resolve(setting));

    public static void Set(GameLanguage language)
    {
        s_current     = language;
        s_initialized = true;
    }

    public static GameLanguage Resolve(string setting)
    {
        switch (setting)
        {
            case "ko": return GameLanguage.Korean;
            case "en": return GameLanguage.English;
            default:   return FromDevice();
        }
    }

    private static GameLanguage FromDevice()
        => Application.systemLanguage == SystemLanguage.Korean ? GameLanguage.Korean : GameLanguage.English;

    // ── 문구 ──────────────────────────────────────────────────

    public static string Get(string key)
    {
        if (s_table.TryGetValue(key, out var e)) return IsKorean ? e.ko : e.en;
        Debug.LogWarning($"[Loc] 번역표에 없는 키: {key}");
        return key;
    }

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

    /// <summary>| 로 나눈 여러 문구 중 하나를 무작위로</summary>
    public static string Pick(string key)
    {
        var options = Get(key).Split('|');
        return options[Random.Range(0, options.Length)];
    }

    /// <summary>문장 주어 — 한국어는 조사까지 ("하코가"), 영어는 이름 그대로</summary>
    public static string Subject(string name)
    {
        if (IsKorean) return KoreanText.WithSubject(name);
        return string.IsNullOrEmpty(name) ? "Your gecko" : name;
    }

    public static string StageName(int stage) => Get("stage." + Mathf.Clamp(stage, 0, 4));

    public static string ItemName(ItemSO item)           => item    == null ? "" : NameOr("item." + item.itemId, item.displayName);
    public static string DecorName(DecorItemSO item)     => item    == null ? "" : NameOr("decor." + item.itemId, item.displayName);
    public static string SpeciesName(GeckoSpeciesSO spe) => spe     == null ? "" : NameOr("species." + spe.speciesId, spe.displayName);

    private static string NameOr(string key, string fallback)
        => s_table.TryGetValue(key, out var e) ? (IsKorean ? e.ko : e.en) : fallback;

    // ── 씬 글자 ───────────────────────────────────────────────

    /// <summary>
    /// text가 번역표의 원문(한국어 또는 영어)과 같으면 현재 언어 문구를 돌려준다.
    /// 표에 없는 글자(코인 수·이름 등)는 false — 건드리지 않는다.
    /// </summary>
    public static bool TryTranslateSource(string text, out string translated)
    {
        translated = null;
        if (string.IsNullOrEmpty(text)) return false;
        if (s_sourceIndex == null) BuildSourceIndex(null);
        if (!s_sourceIndex.TryGetValue(text.Trim(), out var key)) return false;
        translated = Get(key);
        return true;
    }

    // ── 자가 검사용 ───────────────────────────────────────────

    public static IEnumerable<string> Keys => s_table.Keys;

    public static bool TryGetPair(string key, out string ko, out string en)
    {
        bool found = s_table.TryGetValue(key, out var e);
        ko = e.ko;
        en = e.en;
        return found;
    }

    /// <summary>같은 원문이 뜻이 다른 두 키에 걸린 경우 (씬 글자를 어느 쪽으로 바꿀지 모호함)</summary>
    public static List<string> SourceConflicts()
    {
        var conflicts = new List<string>();
        BuildSourceIndex(conflicts);
        return conflicts;
    }

    private static void BuildSourceIndex(List<string> conflicts)
    {
        var index = new Dictionary<string, string>();
        foreach (var kv in s_table)
        {
            // 서식({0})·여러 문구(|)는 씬에 그대로 적힐 수 없으므로 원문 목록에서 뺀다
            foreach (var source in new[] { kv.Value.ko, kv.Value.en })
            {
                if (string.IsNullOrEmpty(source) || source.Contains("{") || source.Contains("|")) continue;
                if (index.TryGetValue(source, out var other))
                {
                    if (other != kv.Key && conflicts != null && s_table[other] != kv.Value)
                        conflicts.Add($"'{source}' — {other} / {kv.Key}");
                    continue;
                }
                index[source] = kv.Key;
            }
        }
        s_sourceIndex = index;
    }
}
