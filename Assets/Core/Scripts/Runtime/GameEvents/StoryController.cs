using UnityEngine;
using UnityEngine.UI; // C?n cái này ð? ði?u khi?n Image và Button
using System.Collections.Generic;

public class StoryController : MonoBehaviour
{
    [Header("Cài ð?t UI")]
    public Image displayImage;      // Kéo ð?i tý?ng Image vào ðây
    public GameObject storyCanvas;  // Kéo ð?i tý?ng Canvas vào ðây
    public GameObject player;       // Kéo nhân v?t Thanhnien1 vào ðây

    [Header("Danh sách ?nh truy?n")]
    public List<Sprite> storySprites; // Kéo các ?nh Sprite ð? chu?n b? vào ðây

    private int currentIndex = 0;

    void Start()
    {
        // Khi m?i vào game: Hi?n truy?n, ?n ngý?i chõi
        storyCanvas.SetActive(true);
        if (player != null) player.SetActive(false);

        ShowImage();
    }

    public void NextImage()
    {
        currentIndex++; // Tãng s? th? t? ?nh lên 1

        if (currentIndex < storySprites.Count)
        {
            ShowImage(); // Hi?n ?nh ti?p theo
        }
        else
        {
            EndStory(); // H?t ?nh th? vào game
        }
    }

    void ShowImage()
    {
        if (storySprites.Count > 0)
        {
            displayImage.sprite = storySprites[currentIndex];
        }
    }

    void EndStory()
    {
        storyCanvas.SetActive(false); // T?t màn h?nh k? chuy?n
        if (player != null) player.SetActive(true); // Hi?n nhân v?t ð? ði?u khi?n
        Debug.Log("B?t ð?u vào Game!");
    }
}