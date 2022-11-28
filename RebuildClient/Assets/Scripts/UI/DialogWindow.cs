﻿using Assets.Scripts.Network;
using Assets.Scripts.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    internal class DialogWindow : WindowBase, IPointerDownHandler
    {
        public TextMeshProUGUI NameBox;
        public TextMeshProUGUI TextBox;
        public Image NpcImage;

        public void SetDialog(string name, string text)
        {
            if(string.IsNullOrWhiteSpace(name))
                NameBox.text = name;
            else
                NameBox.text = $"[{name}]";

            TextBox.text = text;
            
            gameObject.SetActive(true);
        }

        public void ShowImage(string sprite)
        {
            AddressableUtility.LoadSprite(gameObject, "Assets/Sprites/Cutins/" + sprite + ".png", sprite =>
            {
                NpcImage.sprite = sprite;
                NpcImage.SetNativeSize();
                NpcImage.gameObject.SetActive(true);
            });
        }

        public void HideUI()
        {
            NpcImage.gameObject.SetActive(false);
            gameObject.SetActive(false);
            NameBox.text = "";
            TextBox.text = "";
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            NetworkManager.Instance.SendNpcAdvance();
        }
    }
}
