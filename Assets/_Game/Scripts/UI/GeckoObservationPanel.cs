using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Compact, read-only field notes below the existing HUD. Care is routed back to HomeUIController.</summary>
public sealed class GeckoObservationPanel : MonoBehaviour
{
    private TMP_Text _activity, _condition, _milestone, _requirement, _actionText;
    private Image _progress;
    private Button _action;
    private GeckoNeed _need;
    private Action<GeckoNeed> _onCare;
    private const float HEIGHT = 148f;

    public static GeckoObservationPanel Create(RectTransform parent, TMP_FontAsset font, Action<GeckoNeed> onCare, Action onGrowth)
    {
        var root = new GameObject("ObservationPanel", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)root.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.04f, 1f);
        rt.anchorMax = new Vector2(0.96f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -352f); // StatusPanel ends 340 units below the top.
        rt.sizeDelta = new Vector2(0f, HEIGHT);
        HomePresentation.PanelImage(rt);
        var panel = root.AddComponent<GeckoObservationPanel>();
        panel._onCare = onCare;
        Text(rt, font, "Label", 0.025f, 0.47f, 106f, 25f, 18f).text = Loc.Get("observe.label");
        panel._activity = Text(rt, font, "Activity", 0.025f, 0.49f, 69f, 35f, 27f);
        panel._condition = Text(rt, font, "Condition", 0.025f, 0.49f, 18f, 47f, 20f);

        panel._milestone = Text(rt, font, "Milestone", 0.52f, 0.96f, 106f, 25f, 19f);
        panel._requirement = Text(rt, font, "Requirement", 0.52f, 0.96f, 65f, 37f, 22f);
        var growth = panel._requirement.gameObject.AddComponent<Button>();
        panel._requirement.raycastTarget = true;
        growth.onClick.AddListener(() => onGrowth?.Invoke());
        growth.transition = Selectable.Transition.None;

        var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
        var trackRt = (RectTransform)track.transform;
        trackRt.SetParent(rt, false);
        trackRt.anchorMin = new Vector2(0.52f, 0f); trackRt.anchorMax = new Vector2(0.96f, 0f);
        trackRt.sizeDelta = new Vector2(0f, 3f); trackRt.anchoredPosition = new Vector2(0f, 59f);
        track.GetComponent<Image>().color = new Color(0.3f, 0.36f, 0.32f);
        track.GetComponent<Image>().raycastTarget = false;
        var fill = new GameObject("Progress", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(trackRt, false);
        panel._progress = fill.GetComponent<Image>();
        panel._progress.color = new Color(0.67f, 0.74f, 0.52f);
        panel._progress.raycastTarget = false;

        panel._actionText = Text(rt, font, "CareAction", 0.52f, 0.96f, 8f, 43f, 21f);
        panel._actionText.raycastTarget = true;
        panel._action = panel._actionText.gameObject.AddComponent<Button>();
        panel._action.transition = Selectable.Transition.None;
        panel._action.onClick.AddListener(() => panel._onCare?.Invoke(panel._need));
        return panel;
    }

    public void Refresh(GeckoData g, GrowthCheck growth, GeckoActivity activity, bool careAvailable)
    {
        if (g == null) return;
        _need = GeckoObservation.Need(g);
        _activity.text = Loc.Get("observe.activity." + activity);
        _condition.text = Loc.Get("observe.need." + _need);
        float progress;
        if (!growth.IsAdult)
        {
            _milestone.text = Loc.Format("observe.next", Loc.StageName(growth.nextStage));
            if (!growth.HealthMet) _requirement.text = Loc.Format("observe.health", Mathf.FloorToInt(growth.health), Mathf.CeilToInt(growth.needHealth));
            else if (!growth.AffectionMet) _requirement.text = Loc.Format("observe.trust", Mathf.FloorToInt(growth.affection), Mathf.CeilToInt(growth.needAffection));
            else if (!growth.MoltsMet) _requirement.text = Loc.Format("observe.molts", growth.moltCount, growth.needMolts);
            else _requirement.text = Loc.Format("observe.days", growth.ageDays.ToString("0.0"), growth.needDays.ToString("0.#"));
            progress = GrowthProgress(growth);
        }
        else
        {
            int level = GeckoBond.Level(g);
            float next = GeckoBond.NextPoints(level);
            _milestone.text = Loc.Get("observe.adult");
            _requirement.text = next > 0f ? Loc.Format("observe.bond", level + 1, Mathf.FloorToInt(GeckoBond.Points(g)), next)
                : Loc.Format("observe.shed", Mathf.FloorToInt(g.moltProgress));
            progress = next > 0f ? GeckoBond.Points(g) / next : g.moltProgress / 100f;
        }
        var rt = (RectTransform)_progress.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        bool actionable = _need == GeckoNeed.Food || _need == GeckoNeed.Water || _need == GeckoNeed.Clean;
        _action.interactable = actionable && careAvailable;
        _actionText.text = Loc.Get(actionable ? "observe.action." + _need : "observe.no_action");
        _actionText.color = actionable ? new Color(0.82f, 0.84f, 0.63f) : new Color(0.57f, 0.64f, 0.6f);
    }

    public static float GrowthProgress(GrowthCheck c)
    {
        if (c.IsAdult) return 1f;
        float fraction = c.needDays > 0f ? c.ageDays / c.needDays : 1f;
        if (c.needMolts > 0) fraction = Mathf.Min(fraction, (float)c.moltCount / c.needMolts);
        if (c.needHealth > 0f) fraction = Mathf.Min(fraction, c.health / c.needHealth);
        if (c.needAffection > 0f) fraction = Mathf.Min(fraction, c.affection / c.needAffection);
        return Mathf.Clamp01(fraction);
    }

    private static TMP_Text Text(RectTransform parent, TMP_FontAsset font, string name, float left, float right, float y, float height, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(left, 0f); rt.anchorMax = new Vector2(right, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = new Vector2(0f, height);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = size; text.enableAutoSizing = true;
        text.fontSizeMin = size - 3f; text.fontSizeMax = size;
        text.color = new Color(0.9f, 0.91f, 0.85f); text.alignment = TextAlignmentOptions.MidlineLeft;
        text.overflowMode = TextOverflowModes.Ellipsis; text.raycastTarget = false;
        return text;
    }
}
