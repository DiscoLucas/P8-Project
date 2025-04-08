using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReceiptPrinter : MonoBehaviour
{
    public GameObject orderPanelPrefab;
    public Transform printerTransform;

    // Start is called before the first frame update
    void Start()
    {
        PrintReceipt("Order: Pizza", "Order Number: 123", "Time: 12:00 PM", null);
    }

    void Update()
    {
            PrintReceipt("Order: Pizza", "Order Number: 123", "Time: 12:00 PM", null);
    }

    // Method to spawn a receipt panel with provided info
    [ContextMenu("Spawn Receipt")]
    public void PrintReceipt(string order, string orderNum, string time, Sprite image)
    {
        GameObject receiptPanel = Instantiate(orderPanelPrefab, printerTransform.position, Quaternion.identity);
        receiptPanel.transform.SetParent(printerTransform, worldPositionStays: false);
        receiptPanel.transform.localScale = Vector3.one;

        // Get UI elements from the instantiated prefab
        TextMeshProUGUI orderText = receiptPanel.transform.Find("Drink to Order (TMP)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI orderNumber = receiptPanel.transform.Find("Order Number (TMP)")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI orderTime = receiptPanel.transform.Find("Time/Date (TMP)")?.GetComponent<TextMeshProUGUI>();
        Image orderImage = receiptPanel.transform.Find("Drink Image")?.GetComponent<Image>();

        if (orderText != null) orderText.text = order;
        if (orderNumber != null) orderNumber.text = orderNum;
        if (orderTime != null) orderTime.text = time;
        if (orderImage != null && image != null) orderImage.sprite = image;
    }

}
