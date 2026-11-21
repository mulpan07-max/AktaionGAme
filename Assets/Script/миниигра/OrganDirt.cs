using System;
using UnityEngine;

// Хранит "степень грязи" 4 пятен. Общее состояние для всех мест, где виден орган.
// Повесить ОДИН раз на всегда активный объект (например, GameManager).
[DefaultExecutionOrder(-100)]
public class OrganDirt : MonoBehaviour
{
    public static OrganDirt Instance { get; private set; }

    [SerializeField] private int spotCount = 4;
    [Tooltip("На сколько уменьшается непрозрачность за один тап (0..1)")]
    [SerializeField] private float alphaPerTap = 0.2f;
    [Tooltip("Тапы не делают пятно прозрачнее этого значения. Дальше только мытьё.")]
    [SerializeField] private float minAlpha = 0.4f;

    private float[] alpha;
    private bool[] touched;

    public event Action Changed;

    private void Awake()
    {
        Instance = this;
        ResetDirt();
    }

    public float GetAlpha(int i) => (alpha != null && i >= 0 && i < alpha.Length) ? alpha[i] : 1f;

    // Каждое пятно тапнули хотя бы раз
    public bool AllSpotsTouched
    {
        get { foreach (bool t in touched) if (!t) return false; return true; }
    }

    // Все пятна полностью исчезли
    public bool IsClean
    {
        get { foreach (float a in alpha) if (a > 0.001f) return false; return true; }
    }

    // Тап по пятну: чуть прозрачнее, но не ниже minAlpha
    public void Tap(int i)
    {
        if (i < 0 || i >= alpha.Length) return;
        touched[i] = true;
        if (alpha[i] > minAlpha) alpha[i] = Mathf.Max(minAlpha, alpha[i] - alphaPerTap);
        Changed?.Invoke();
    }

    // Мытьё: уменьшает прозрачность ВСЕХ пятен до нуля
    public void Scrub(float amount)
    {
        for (int i = 0; i < alpha.Length; i++) alpha[i] = Mathf.Max(0f, alpha[i] - amount);
        Changed?.Invoke();
    }

    // Вызывать в начале новой операции/главы
    public void ResetDirt()
    {
        alpha = new float[spotCount];
        touched = new bool[spotCount];
        for (int i = 0; i < spotCount; i++) alpha[i] = 1f;
        Changed?.Invoke();
    }
}