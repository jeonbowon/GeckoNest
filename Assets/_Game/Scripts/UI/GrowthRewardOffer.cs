using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>성장 연출과 결과 알림이 끝난 뒤 한 번 제안하는 선택형 광고 축하 보상.</summary>
public sealed class GrowthRewardOffer : MonoBehaviour
{
    private RewardManager _reward;
    private AdMobService _ads;
    private string _geckoId;
    private int _growthStage;
    private TMP_Text _description;
    private Button _watchButton;
    private TMP_Text _watchText;
    private Button _closeButton;
    private bool _finished;

    public static IEnumerator Present(RectTransform root, TMP_FontAsset font, string geckoId, int growthStage)
    {
        if (root == null || GameManager.Instance == null) yield break;
        var reward = GameManager.Instance.Reward;
        if (reward == null || !reward.CanClaimGrowthAd(geckoId, growthStage)) yield break;

        var offer = Create(root, font, reward, geckoId, growthStage);
        while (offer != null && !offer._finished) yield return null;
    }

    private static GrowthRewardOffer Create(RectTransform root, TMP_FontAsset font, RewardManager reward,
                                             string geckoId, int growthStage)
    {
        var overlayGo = new GameObject("GrowthRewardOffer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var overlayRt = (RectTransform)overlayGo.transform;
        overlayRt.SetParent(root, false);
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
        overlayGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.64f);
        overlayRt.SetAsLastSibling();

        var cardGo = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var cardRt = (RectTransform)cardGo.transform;
        cardRt.SetParent(overlayRt, false);
        cardRt.anchorMin = new Vector2(0.10f, 0.34f);
        cardRt.anchorMax = new Vector2(0.90f, 0.66f);
        cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
        cardGo.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.15f, 0.98f);

        var offer = overlayGo.AddComponent<GrowthRewardOffer>();
        offer._reward = reward;
        offer._ads = AdMobService.Instance;
        offer._geckoId = geckoId;
        offer._growthStage = growthStage;

        var title = NewText(cardRt, "Title", new Vector2(0.07f, 0.72f), new Vector2(0.93f, 0.94f), 38f, font);
        title.text = Loc.Format("ad.growth_title", Loc.StageName(growthStage));
        title.fontStyle = FontStyles.Bold;

        offer._description = NewText(cardRt, "Description", new Vector2(0.07f, 0.48f), new Vector2(0.93f, 0.72f), 29f, font);
        offer._description.text = Loc.Format("ad.growth_desc", RewardManager.GROWTH_AD_REWARD_COIN);

        offer._watchButton = NewButton(cardRt, "WatchButton", new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.43f),
                                       new Color(0.24f, 0.46f, 0.31f, 1f), font, out offer._watchText);
        offer._watchButton.onClick.AddListener(offer.OnWatch);

        offer._closeButton = NewButton(cardRt, "CloseButton", new Vector2(0.31f, 0.04f), new Vector2(0.69f, 0.16f),
                                       new Color(0.22f, 0.27f, 0.23f, 1f), font, out var closeText);
        closeText.text = Loc.Get("common.close");
        offer._closeButton.onClick.AddListener(offer.Close);

        if (offer._ads != null) offer._ads.StateChanged += offer.Refresh;
        offer.Refresh();
        return offer;
    }

    private void OnWatch()
    {
        if (_ads == null || _reward == null || !_reward.CanClaimGrowthAd(_geckoId, _growthStage)) return;
        _watchButton.interactable = false;
        _watchText.text = Loc.Get("ad.showing");
        _ads.ShowRewarded(
            onEarned: () =>
            {
                int coin = _reward != null ? _reward.ClaimGrowthAd(_geckoId, _growthStage) : 0;
                if (coin <= 0) return;
                AudioManager.Play(Sfx.Sparkle, 0.8f);
                Haptics.Success();
                _description.text = Loc.Format("ad.rewarded", coin);
                _watchButton.gameObject.SetActive(false);
                StartCoroutine(CloseAfterDelay());
            },
            onClosed: Refresh,
            onUnavailable: () =>
            {
                if (_description != null) _description.text = Loc.Get("ad.unavailable");
                Refresh();
            });
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        Close();
    }

    private void Refresh()
    {
        if (_watchButton == null || _watchText == null || _reward == null) return;
        if (!_reward.CanClaimGrowthAd(_geckoId, _growthStage))
        {
            _watchButton.gameObject.SetActive(false);
            return;
        }

        bool ready = _ads != null && _ads.IsReady;
        _watchButton.interactable = ready;
        _watchText.text = ready
            ? Loc.Format("ad.growth_watch", RewardManager.GROWTH_AD_REWARD_COIN)
            : (_ads != null && _ads.State == AdMobService.AdState.Unavailable
                ? Loc.Get("ad.unavailable") : Loc.Get("ad.loading"));
    }

    private void Close()
    {
        if (_finished) return;
        _finished = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_ads != null) _ads.StateChanged -= Refresh;
    }

    private static TMP_Text NewText(RectTransform parent, string name, Vector2 min, Vector2 max,
                                    float size, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.62f;
        text.fontSizeMax = size;
        text.color = new Color(0.94f, 0.97f, 0.92f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button NewButton(RectTransform parent, string name, Vector2 min, Vector2 max,
                                    Color color, TMP_FontAsset font, out TMP_Text label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        UIPressScale.Ensure(button);
        label = NewText(rt, "Text", Vector2.zero, Vector2.one, 30f, font);
        label.fontStyle = FontStyles.Bold;
        return button;
    }
}
