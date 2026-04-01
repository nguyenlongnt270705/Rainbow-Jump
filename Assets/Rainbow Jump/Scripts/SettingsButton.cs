using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// SettingsButton.cs: quản lý menu settings và mute/unmute audio
namespace RainbowJump.Scripts
{

    public class SettingsButton : MonoBehaviour
    {
        public bool muted;

        // Flow chính: bật/tắt nút settings, mute/unmute và lưu PlayerPrefs
        public GameObject buttons;
        public GameObject muteButton;
        public GameObject unMuteButton;
        public Animator buttonsAnimator;
        public AnimationClip newAnimationClip;
        public int timesClicked = 0;
        // Flow: mở/đóng khung settings
        public void SettingsButtonClick()
        {
            timesClicked++;
            if (timesClicked % 2 == 0)
            {
                buttonsAnimator.Play(newAnimationClip.name);
                Invoke("DisableButtons", 0.35f);
            }
            else
            {
                buttons.SetActive(true);
            }
        }

        // Flow: ẩn buttons settings
        private void DisableButtons()
        {
            buttons.SetActive(false);
        }

        // Flow: bật chế độ mute (volume 0) và lưu trạng thái
        public void UnMuteButtonClick()
        {
            muted = true;
            unMuteButton.SetActive(false);
            muteButton.SetActive(true);
            PlayerPrefs.SetInt("muted", muted ? 1 : 0);
            AudioListener.volume = 0f;
        }

        // Flow: tắt chế độ mute (volume 1) và lưu trạng thái
        public void MuteButtonClick()
        {
            muted = false;
            unMuteButton.SetActive(true);
            muteButton.SetActive(false);
            PlayerPrefs.SetInt("muted", muted ? 1 : 0);
            AudioListener.volume = 1f;
        }

        // Flow: khởi tạo trạng thái mute theo PlayerPrefs
        void Start()
        {
            muted = PlayerPrefs.GetInt("muted", 0) == 1;
            if (muted)
            {
                UnMuteButtonClick();
            }
            else
            {
                MuteButtonClick();
            }
        }

    }
}
