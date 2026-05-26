using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Spawns customers into waiting chairs (queue). Uses a dailyQueue of orders (CustomerOrder).
/// Attach instance in Lobby scene and assign waiting chairs and customerPrefab.
/// CustomerController (on prefab) should handle pointer clicks and call Dialog flow.
/// </summary>
public class CustomerManager : Singleton<CustomerManager>
{
    [Tooltip("Transforms for 3 waiting chairs")]
    public Transform[] waitingChairs = new Transform[3];
    public GameObject customerPrefab;
    [Tooltip("Template orders: create some CustomerOrder objects in inspector or create via code")]
    public List<CustomerOrder> orderTemplates = new List<CustomerOrder>();

    private Queue<CustomerOrder> dailyQueue = new Queue<CustomerOrder>();
    private List<CustomerController> activeCustomers = new List<CustomerController>();
    private int totalToday = 0;
    private bool spawning = false;

    public event Action OnAllCustomersServed;

    public void StartDay()
    {
        // Ensure we have patterns seeded in database
        // Determine customer count
        totalToday = WeatherSystem.Instance.CalculateCustomerCount(
                        GameManager.Instance.currentSave.currentDayOfWeek,
                        GameManager.Instance.currentSave.todayWeather);
        GenerateDailyOrders(totalToday);
        spawning = true;
        StartCoroutine(SpawnLoop());
    }

    private void GenerateDailyOrders(int count)
    {
        dailyQueue.Clear();
        if (orderTemplates.Count == 0)
        {
            Debug.LogWarning("[CustomerManager] No order templates assigned.");
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var template = orderTemplates[UnityEngine.Random.Range(0, orderTemplates.Count)];
            // Create a shallow clone (if needed you can deep copy)
            var clone = new CustomerOrder()
            {
                customerName = template.customerName,
                desiredPattern = template.desiredPattern,
                desiredColor = template.desiredColor,
                backStory = template.backStory,
                requestDialog = template.requestDialog
            };
            dailyQueue.Enqueue(clone);
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (dailyQueue.Count > 0 && spawning)
        {
            // Wait until there is an empty chair
            while (GetEmptyChairIndex() == -1)
            {
                yield return null;
            }

            // spawn interval (can be tuned based on day/weather)
            float wait = UnityEngine.Random.Range(6f, 16f);
            yield return new WaitForSeconds(wait);

            SpawnCustomerToChair(GetEmptyChairIndex());
        }
    }

    private int GetEmptyChairIndex()
    {
        for (int i = 0; i < waitingChairs.Length; i++)
            if (waitingChairs[i].childCount == 0) return i;
        return -1;
    }

    private void SpawnCustomerToChair(int chairIndex)
    {
        if (chairIndex < 0 || dailyQueue.Count == 0) return;
        var order = dailyQueue.Dequeue();
        Transform chair = waitingChairs[chairIndex];
        GameObject go = Instantiate(customerPrefab, chair);
        go.transform.localPosition = Vector3.zero;
        var ctrl = go.GetComponent<CustomerController>();
        ctrl.SetOrder(order);
        activeCustomers.Add(ctrl);
    }

    public void RemoveCustomer(CustomerController ctrl)
    {
        if (activeCustomers.Contains(ctrl)) activeCustomers.Remove(ctrl);
        Destroy(ctrl.gameObject);
        // check if all done
        if (dailyQueue.Count == 0 && activeCustomers.Count == 0)
        {
            spawning = false;
            OnAllCustomersServed?.Invoke();
        }
    }
}