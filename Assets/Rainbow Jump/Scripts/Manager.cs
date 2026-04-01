using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Manager.cs: quản lý toàn bộ trạng thái game, UI, score, high score, âm thanh, achievement và scene flow.
namespace RainbowJump.Scripts
{
    public class Manager : MonoBehaviour
    {

        // Flow tổng: Start setup, Update liên tục check gameover/score/achievement, TapToStart và RestartGame điều khiển vòng đời gameplay
        public string androidUrl;
        public string iOSUrl;

        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public Player playerMovement;
        public TrailRenderer playerTrail;
        public Spawner spawner;
        public GameObject mainCamera;

        public Transform playerTransform;
        public Text scoreText;
        public Text highScoreText;

        public float score = 0f;

        private const int HighScoreSlots = 5;
        private const string PrefsKeyHighScores = "HighScoresTop5";
        private const string PrefsKeyLegacyHighScore = "HighScore";
        private readonly List<float> _highScores = new List<float>(HighScoreSlots);

        public bool gameOver = false;

        public SpriteRenderer playerSprite;

        public GameObject playerScore;
        public GameObject deathParticle;
        public GameObject gameOverUI;
        public GameObject tapToPlayUI;
        public GameObject tapToStartBtn;
        public GameObject settingsButton;
        public GameObject settingsButtons;
        public SettingsButton settingsButtonScript;

        public Text achievementNoticeText;
        public GameObject highScoreBoardPanel;

        public AudioClip tapSound;
        public AudioClip deathSound;
        public AudioClip buttonSound;
        private AudioSource audioSource;

        private const int AchievementStepPoints = 20;
        private int _nextAchievementMilestone = AchievementStepPoints;

        private Text _achievementNoticeRuntime;
        private Coroutine _achievementNoticeCoroutine;

        private GameObject _highScoreBoardRuntimeRoot;
        private Text _highScoreBoardScoreLine;


        void Awake()
        {
            WireHighScoreStarButton();
        }

        /// <summary>
        /// </summary>
        private void WireHighScoreStarButton()
        {
            if (settingsButtons == null) return;

            Transform starRoot = settingsButtons.transform.Find("RateUsButton");
            if (starRoot == null)
            {
                for (var i = 0; i < settingsButtons.transform.childCount; i++)
                {
                    var ch = settingsButtons.transform.GetChild(i);
                    if (ch.name.IndexOf("RateUs", System.StringComparison.Ordinal) >= 0)
                    {
                        starRoot = ch;
                        break;
                    }
                }
            }

            if (starRoot == null) return;

            var buttonTransform = starRoot.Find("Button");
            if (buttonTransform == null) return;

            var btn = buttonTransform.GetComponent<Button>();
            if (btn == null) return;

            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(ToggleHighScoreBoard);
            btn.onClick.AddListener(PlayButtonSound);
        }

        // Start flow: thiết lập ban đầu, tắt player, load audio/score
        void Start()
        {
            Application.targetFrameRate = 144;
            playerMovement.enabled = false;
            playerMovement.rb.simulated = false;

            audioSource = GetComponent<AudioSource>();

            LoadHighScoresFromPrefs();
            ApplyHighScoreTexts();
        }

        // Update flow: kiểm tra gameOver, cập nhật điểm, achievement
        void Update()
        {
            if (gameOver == true)
            {
                playerMovement.enabled = false;
                playerMovement.rb.simulated = false;
                playerScore.SetActive(false);
                playerSprite.enabled = false;
                tapToStartBtn.SetActive(false);
                gameOverUI.SetActive(true);
                deathParticle.SetActive(true);
                gameOver = false;

                SubmitRunToLeaderboard(score);
            }

            if (playerTransform.position.y > score)
            {
                // cập nhật điểm nếu player đạt cao hơn điểm hiện tại
                score = playerTransform.position.y;
            }

            scoreText.text = (score).ToString("0");

            if (playerMovement.enabled)
                CheckScoreAchievements();
        }

        private void CheckScoreAchievements()
        {
            while (score >= _nextAchievementMilestone)
            {
                var milestone = _nextAchievementMilestone;
                _nextAchievementMilestone += AchievementStepPoints;
                OnAchievementUnlocked(milestone);
            }
        }

        private void OnAchievementUnlocked(int milestonePoints)
        {
            PlayAchievementBurst();
            ShowAchievementNotice(milestonePoints);
        }

        private void PlayAchievementBurst()
        {
            if (deathParticle == null) return;

            var pos = playerTransform.position;
            var burst = Instantiate(deathParticle, pos, Quaternion.identity);
            burst.transform.SetParent(null);
            burst.SetActive(true);

            foreach (var ps in burst.GetComponentsInChildren<ParticleSystem>(true))
            {
                ApplyAchievementBrightRandomColors(ps);
                ps.Clear(true);
                ps.Play(true);
            }

            PlayTapSound();
            Destroy(burst, 1.5f);
        }

        private void ShowAchievementNotice(int milestonePoints)
        {
            var label = AchievementNoticeLabel;
            if (label == null) return;

            if (_achievementNoticeCoroutine != null)
                StopCoroutine(_achievementNoticeCoroutine);
            _achievementNoticeCoroutine = StartCoroutine(AchievementNoticeRoutine(label, milestonePoints));
        }

        private Text AchievementNoticeLabel
        {
            get
            {
                if (achievementNoticeText != null)
                    return achievementNoticeText;
                if (_achievementNoticeRuntime != null)
                    return _achievementNoticeRuntime;
                _achievementNoticeRuntime = CreateAchievementNoticeText();
                return _achievementNoticeRuntime;
            }
        }

        private Text CreateAchievementNoticeText()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return null;

            var go = new GameObject("AchievementNotice");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.58f);
            rt.anchorMax = new Vector2(0.5f, 0.58f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(720f, 100f);

            var txt = go.AddComponent<Text>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                txt.font = font;
            txt.fontSize = 46;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);

            go.SetActive(false);
            return txt;
        }

        private IEnumerator AchievementNoticeRoutine(Text label, int milestonePoints)
        {
            var rt = label.rectTransform;
            label.gameObject.SetActive(true);
            label.text = milestonePoints + " points!";
            label.color = Color.white;

            const float popIn = 0.22f;
            const float hold = 0.75f;
            const float fadeOut = 0.45f;
            var t = 0f;
            while (t < popIn)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / popIn);
                var ease = 1f - (1f - k) * (1f - k);
                rt.localScale = Vector3.LerpUnclamped(Vector3.one * 0.5f, Vector3.one * 1.08f, ease);
                yield return null;
            }

            rt.localScale = Vector3.one;
            yield return new WaitForSeconds(hold);

            t = 0f;
            var c = label.color;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                c.a = 1f - Mathf.Clamp01(t / fadeOut);
                label.color = c;
                rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.92f, t / fadeOut);
                yield return null;
            }

            c.a = 0f;
            label.color = c;
            label.gameObject.SetActive(false);
            rt.localScale = Vector3.one;
            _achievementNoticeCoroutine = null;
        }

        private static Gradient BuildBrightRandomColorPickGradient()
        {
            const int keyCount = 8;
            var colorKeys = new GradientColorKey[keyCount];
            for (var i = 0; i < keyCount; i++)
            {
                var t = keyCount == 1 ? 0f : i / (float)(keyCount - 1);
                colorKeys[i] = new GradientColorKey(RandomBrightSaturatedColor(), t);
            }

            var alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            };
            var grad = new Gradient();
            grad.SetKeys(colorKeys, alphaKeys);
            return grad;
        }

        private static Color RandomBrightSaturatedColor()
        {
            return Random.ColorHSV(0f, 1f, 0.72f, 1f, 0.88f, 1f, 1f, 1f);
        }

        private static void ApplyAchievementBrightRandomColors(ParticleSystem ps)
        {
            var pickGradient = BuildBrightRandomColorPickGradient();
            var start = new ParticleSystem.MinMaxGradient
            {
                mode = ParticleSystemGradientMode.RandomColor,
                gradient = pickGradient
            };
            var main = ps.main;
            main.startColor = start;

            var overLife = ps.colorOverLifetime;
            overLife.enabled = true;
            var neutralFade = new Gradient();
            neutralFade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            overLife.color = new ParticleSystem.MinMaxGradient(neutralFade);
        }

        // Flow: bắt đầu chơi khi tap to start
        public void TapToStart()
        {
            settingsButtonScript.timesClicked = 0;
            PlayTapSound();
            playerMovement.rb.simulated = true;
            playerMovement.rb.velocity = Vector2.up * playerMovement.jumpForce;
            playerScore.SetActive(true);
            tapToPlayUI.SetActive(false);
            settingsButton.SetActive(false);
            settingsButtons.SetActive(false);
            tapToStartBtn.SetActive(false);
            playerMovement.enabled = true;
            HideHighScoreBoard();
        }

        // Flow: mở/đóng bảng high score
        public void ToggleHighScoreBoard()
        {
            var root = HighScoreBoardRoot;
            if (root == null) return;

            if (root.activeSelf)
                HideHighScoreBoard();
            else
            {
                RefreshHighScoreBoardDisplay();
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }
        }

        public void HideHighScoreBoard()
        {
            var root = highScoreBoardPanel != null ? highScoreBoardPanel : _highScoreBoardRuntimeRoot;
            if (root != null)
                root.SetActive(false);
        }

        private GameObject HighScoreBoardRoot
        {
            get
            {
                if (highScoreBoardPanel != null)
                    return highScoreBoardPanel;
                if (_highScoreBoardRuntimeRoot != null)
                    return _highScoreBoardRuntimeRoot;
                _highScoreBoardRuntimeRoot = CreateHighScoreBoardUI();
                return _highScoreBoardRuntimeRoot;
            }
        }

        private void RefreshHighScoreBoardDisplay()
        {
            LoadHighScoresFromPrefs();
            ApplyHighScoreTexts();
        }

        private void LoadHighScoresFromPrefs()
        {
            _highScores.Clear();
            var data = PlayerPrefs.GetString(PrefsKeyHighScores, "");
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var token in data.Split(';'))
                {
                    if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                        _highScores.Add(v);
                }
            }

            if (_highScores.Count == 0)
            {
                var legacy = PlayerPrefs.GetFloat(PrefsKeyLegacyHighScore, 0f);
                if (legacy > 0f)
                    _highScores.Add(legacy);
            }

            SortAndTrimLeaderboard();
        }

        private void SortAndTrimLeaderboard()
        {
            _highScores.Sort((a, b) => b.CompareTo(a));
            while (_highScores.Count > HighScoreSlots)
                _highScores.RemoveAt(_highScores.Count - 1);
        }

        private void PersistHighScores()
        {
            SortAndTrimLeaderboard();
            if (_highScores.Count == 0)
            {
                PlayerPrefs.DeleteKey(PrefsKeyHighScores);
                PlayerPrefs.SetFloat(PrefsKeyLegacyHighScore, 0f);
            }
            else
            {
                var sb = new StringBuilder();
                for (var i = 0; i < _highScores.Count; i++)
                {
                    if (i > 0) sb.Append(';');
                    sb.Append(_highScores[i].ToString("G9", CultureInfo.InvariantCulture));
                }

                PlayerPrefs.SetString(PrefsKeyHighScores, sb.ToString());
                PlayerPrefs.SetFloat(PrefsKeyLegacyHighScore, _highScores[0]);
            }

            PlayerPrefs.Save();
        }

        private void SubmitRunToLeaderboard(float runScore)
        {
            _highScores.Add(runScore);
            PersistHighScores();
            ApplyHighScoreTexts();
        }

        private static string FormatLeaderboardForUi(IReadOnlyList<float> scores)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < HighScoreSlots; i++)
            {
                var line = i < scores.Count ? scores[i].ToString("0", CultureInfo.InvariantCulture) : "—";
                sb.AppendLine((i + 1) + ". " + line);
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatGameOverBestScoreLine(IReadOnlyList<float> scores)
        {
            var best = scores.Count > 0 ? scores[0] : 0f;
            return "High score: " + best.ToString("0", CultureInfo.InvariantCulture);
        }

        private void ApplyHighScoreTexts()
        {
            if (highScoreText != null)
                highScoreText.text = FormatGameOverBestScoreLine(_highScores);
            if (_highScoreBoardScoreLine != null)
                _highScoreBoardScoreLine.text = FormatLeaderboardForUi(_highScores);
        }

        private GameObject CreateHighScoreBoardUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return null;

            var font = highScoreText != null && highScoreText.font != null
                ? highScoreText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject("HighScoreBoard");
            root.transform.SetParent(canvas.transform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;
            var dimBtn = root.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(HideHighScoreBoard);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(440f, 400f);

            var panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.14f, 0.98f);
            panelBg.raycastTarget = true;

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f);
            titleRt.sizeDelta = new Vector2(380f, 50f);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;
            titleTxt.text = "LEADERBOARD";

            var scoreGo = new GameObject("Score");
            scoreGo.transform.SetParent(panel.transform, false);
            var scoreRt = scoreGo.AddComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRt.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRt.pivot = new Vector2(0.5f, 0.5f);
            scoreRt.anchoredPosition = new Vector2(0f, 4f);
            scoreRt.sizeDelta = new Vector2(400f, 260f);
            _highScoreBoardScoreLine = scoreGo.AddComponent<Text>();
            _highScoreBoardScoreLine.font = font;
            _highScoreBoardScoreLine.fontSize = 34;
            _highScoreBoardScoreLine.fontStyle = FontStyle.Bold;
            _highScoreBoardScoreLine.alignment = TextAnchor.MiddleCenter;
            _highScoreBoardScoreLine.color = new Color(1f, 0.92f, 0.35f);
            _highScoreBoardScoreLine.raycastTarget = false;
            _highScoreBoardScoreLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            _highScoreBoardScoreLine.verticalOverflow = VerticalWrapMode.Overflow;

            var closeGo = new GameObject("Close");
            closeGo.transform.SetParent(panel.transform, false);
            var closeRt = closeGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 20f);
            closeRt.sizeDelta = new Vector2(200f, 48f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() =>
            {
                PlayButtonSound();
                HideHighScoreBoard();
            });
            var closeLabelGo = new GameObject("Label");
            closeLabelGo.transform.SetParent(closeGo.transform, false);
            var closeLabelRt = closeLabelGo.AddComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            var closeTxt = closeLabelGo.AddComponent<Text>();
            closeTxt.font = font;
            closeTxt.fontSize = 22;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            closeTxt.text = "CLOSE";

            ApplyHighScoreTexts();

            root.SetActive(false);
            return root;
        }

        // Flow: reset game state khi restart
        public void RestartGame()
        {
            settingsButton.SetActive(true);
            tapToPlayUI.SetActive(true);
            tapToStartBtn.SetActive(true);
            playerSprite.enabled = true;
            deathParticle.SetActive(false);
            gameOverUI.SetActive(false);
            playerMovement.rb.simulated = false;
            playerMovement.transform.position = new Vector3(0f, -3f, 0f);
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            spawner.DestroyAllObstacles();
            spawner.InitializeObstacles();
            score = 0f;

            _nextAchievementMilestone = AchievementStepPoints;

            if (_achievementNoticeCoroutine != null)
            {
                StopCoroutine(_achievementNoticeCoroutine);
                _achievementNoticeCoroutine = null;
            }

            var notice = achievementNoticeText != null ? achievementNoticeText : _achievementNoticeRuntime;
            if (notice != null)
            {
                notice.gameObject.SetActive(false);
                var c = notice.color;
                c.a = 1f;
                notice.color = c;
                notice.rectTransform.localScale = Vector3.one;
            }

            HideHighScoreBoard();

            playerTrail.Clear();
        }

        public void PlayTapSound()
        {
            if (audioSource != null && tapSound != null)
                audioSource.PlayOneShot(tapSound);
        }

        public void PlayDeathSound()
        {
            if (audioSource != null && deathSound != null)
                audioSource.PlayOneShot(deathSound);
        }

        public void PlayButtonSound()
        {
            if (audioSource != null && buttonSound != null)
                audioSource.PlayOneShot(buttonSound);
        }

        public void OpenURL()
        {
#if UNITY_ANDROID
        Application.OpenURL(androidUrl);
#elif UNITY_IOS
        Application.OpenURL(iOSUrl);
#endif
        }

        public void ReturnToMainMenu()
        {
            PlayButtonSound();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}