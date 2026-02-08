using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelPhysics
{
    public class PixelPhysicsUI : MonoBehaviour
    {
        public PixelPhysicsManagerGPU manager;

        public Transform container;
        public Transform itemUIPrefab;
        private void Start()
        {
            for (int i = 0; i < manager.powders.Length; i++)
            {
                int ii = i;
                var item = Instantiate(itemUIPrefab, container);
                item.GetChild(0).GetComponent<Image>().color = manager.powders[i];
                item.GetComponent<Button>().onClick.AddListener(delegate { manager.selectedItem = ii; });
            }
            
            for (int i = 0; i < manager.solids.Length; i++)
            {
                int ii = i;
                var item = Instantiate(itemUIPrefab, container);
                item.GetChild(0).GetComponent<Image>().color = manager.solids[i];
                item.GetComponent<Button>().onClick.AddListener(delegate { manager.selectedItem = ii+manager.powders.Length; });
            }
        }
    }
}