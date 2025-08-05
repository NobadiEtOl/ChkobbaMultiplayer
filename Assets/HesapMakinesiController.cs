using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class HesapMakinesiController : MonoBehaviour
{
    [Header("Idle Animation")]
    [SerializeField] private Sprite[] idleAnimationFrames;
    [SerializeField] private float idleFrameRate = 10f;
    
    [Header("Movement")]
    [SerializeField] private Transform reachPoint;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    
    [Header("Coin Interaction")]
    [SerializeField] private float coinDistanceThreshold = 1f; // Distance from start to trigger movement
    
    [Header("Calculator UI")]
    [SerializeField] private TextMeshProUGUI displayText; // Calculator screen
    [SerializeField] private TextMeshProUGUI equalsButtonText; // = button text
    [SerializeField] private TextMeshProUGUI coinDisplayText; // = button text
    [SerializeField] private Button[] numberButtons; // Buttons 0-9
    [SerializeField] private Button multiplyButton; // x button
    [SerializeField] private Button addButton; // + button
    [SerializeField] private Button clearButton; // C button
    [SerializeField] private Button equalsButton; // = button
    
    // Store all calculator buttons for detection
    private List<Button> allCalculatorButtons = new List<Button>();
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private Vector3 startingPosition;
    private int currentFrame = 0;
    private float timer = 0f;
    private bool isMoving = false;
    private bool isAtReachPoint = false;
    private Sequence moveSequence;
    
    // Coin tracking
    private GameObject currentCoin;
    private Vector3 coinStartPosition;
    private bool hasMovedToReachPoint = false;
    
    // Calculator variables
    private string currentInput = "";
    private string currentExpression = "";
    private bool lastWasOperator = false;
    private bool lastWasNumber = false;
    
    // Token data structure - using KeseController's TokenData
    // [System.Serializable]
    // public class TokenData
    // {
    //     public int value;
    //     public int count;
    //     
    //     public TokenData(int value, int count)
    //     {
    //         this.value = value;
    //         this.count = count;
    //     }
    // }
    
    void Start()
    {
        // Get the SpriteRenderer component for sprite animation
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("HesapMakinesiController: No SpriteRenderer component found!");
            return;
        }
        
        // Store the starting position
        startingPosition = transform.position;
        
        // Set initial sprite
        if (idleAnimationFrames != null && idleAnimationFrames.Length > 0)
        {
            spriteRenderer.sprite = idleAnimationFrames[0];
        }
        
        // Find the coin in the scene
        FindCoinInScene();
        
        // Store coin start position
        if (currentCoin != null)
        {
            coinStartPosition = currentCoin.transform.position;
        }
        
        // Setup calculator
        SetupCalculator();
    }
    
    void Update()
    {
        // Play idle animation
        PlayIdleAnimation();
    }
    
    /// <summary>
    /// Plays the idle animation by cycling through the sprite frames
    /// </summary>
    private void PlayIdleAnimation()
    {
        if (idleAnimationFrames == null || idleAnimationFrames.Length == 0) return;
        
        timer += Time.deltaTime;
        if (timer >= 1f / idleFrameRate)
        {
            currentFrame = (currentFrame + 1) % idleAnimationFrames.Length;
            if (spriteRenderer != null)
                spriteRenderer.sprite = idleAnimationFrames[currentFrame];
            timer = 0f;
        }
    }
    
    /// <summary>
    /// Finds the coin object in the scene
    /// </summary>
    private void FindCoinInScene()
    {
        // Look for objects with "Coin" in their name or tag
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("coin") || 
                (obj.tag != null && obj.tag.ToLower().Contains("coin")))
            {
                currentCoin = obj;
                Debug.Log($"HesapMakinesiController: Found coin: {obj.name}");
                break;
            }
        }
        
        if (currentCoin == null)
        {
            Debug.LogWarning("HesapMakinesiController: No coin found in scene!");
        }
    }
    
    /// <summary>
    /// Called by KeseController when a quick drop is detected
    /// </summary>
    public void OnQuickDropDetected()
    {
        Debug.Log("HesapMakinesiController: Quick drop detected! Moving to reach point");
        hasMovedToReachPoint = true;
        MoveToReachPoint();
        
        // Clear calculator when opened
        ClearCalculator();
    }
    

    
    /// <summary>
    /// Called by KeseController to force close the hesap makinesi
    /// </summary>
    public void ForceClose()
    {
        Debug.Log("HesapMakinesiController: Force closing hesap makinesi!");
        hasMovedToReachPoint = false;
        MoveToStartingPosition();
    }
    
    /// <summary>
    /// Public method to manually open the hesap makinesi (for testing)
    /// </summary>
    [ContextMenu("Open Hesap Makinesi")]
    public void ManualOpen()
    {
        Debug.Log("HesapMakinesiController: Manually opening hesap makinesi!");
        hasMovedToReachPoint = true;
        MoveToReachPoint();
    }
    
    /// <summary>
    /// Public method to manually close the hesap makinesi (for testing)
    /// </summary>
    [ContextMenu("Close Hesap Makinesi")]
    public void ManualClose()
    {
        Debug.Log("HesapMakinesiController: Manually closing hesap makinesi!");
        hasMovedToReachPoint = false;
        MoveToStartingPosition();
    }
    
    // ===== CALCULATOR METHODS =====
    
    /// <summary>
    /// Sets up the calculator buttons and initial state
    /// </summary>
    private void SetupCalculator()
    {
        // Setup number buttons (0-9)
        if (numberButtons != null && numberButtons.Length >= 10)
        {
            for (int i = 0; i < 10; i++)
            {
                int number = i; // Capture the value for the lambda
                if (numberButtons[i] != null)
                {
                    numberButtons[i].onClick.AddListener(() => OnNumberPressed(number));
                    allCalculatorButtons.Add(numberButtons[i]);
                }
            }
        }
        
        // Setup operator buttons
        if (multiplyButton != null)
        {
            multiplyButton.onClick.AddListener(OnMultiplyPressed);
            allCalculatorButtons.Add(multiplyButton);
        }
        if (addButton != null)
        {
            addButton.onClick.AddListener(OnAddPressed);
            allCalculatorButtons.Add(addButton);
        }
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearPressed);
            allCalculatorButtons.Add(clearButton);
        }
        if (equalsButton != null)
        {
            equalsButton.onClick.AddListener(OnEqualsPressed);
            allCalculatorButtons.Add(equalsButton);
        }
        
        // Initialize display
        UpdateDisplay();
    }
    
    /// <summary>
    /// Called when a number button is pressed
    /// </summary>
    private void OnNumberPressed(int number)
    {
        Debug.Log($"HesapMakinesiController: Number {number} pressed");
        
        // Add number to current input
        currentInput += number.ToString();
        currentExpression += number.ToString();
        
        lastWasNumber = true;
        lastWasOperator = false;
        
        UpdateDisplay();
        UpdateEqualsText();
    }
    
    /// <summary>
    /// Called when multiply (x) button is pressed
    /// </summary>
    private void OnMultiplyPressed()
    {
        Debug.Log("HesapMakinesiController: Multiply (x) button pressed");
        
        // Only allow multiply if last input was a number
        if (lastWasNumber && !lastWasOperator)
        {
            currentExpression += " x ";
            lastWasOperator = true;
            lastWasNumber = false;
            UpdateDisplay();
            UpdateEqualsText();
        }
        else
        {
            Debug.Log("HesapMakinesiController: Invalid input - cannot use x after operator");
            UpdateEqualsText();
        }
    }
    
    /// <summary>
    /// Called when add (+) button is pressed
    /// </summary>
    private void OnAddPressed()
    {
        Debug.Log("HesapMakinesiController: Add (+) button pressed");
        
        // Only allow add if last input was a number
        if (lastWasNumber && !lastWasOperator)
        {
            currentExpression += " + ";
            lastWasOperator = true;
            lastWasNumber = false;
            UpdateDisplay();
            UpdateEqualsText();
        }
        else
        {
            Debug.Log("HesapMakinesiController: Invalid input - cannot use + after operator");
            UpdateEqualsText();
        }
    }
    
    /// <summary>
    /// Called when clear (C) button is pressed
    /// </summary>
    private void OnClearPressed()
    {
        Debug.Log("HesapMakinesiController: Clear (C) button pressed");
        
        ClearCalculator();
        UpdateEqualsText();
    }
    
    /// <summary>
    /// Clears the calculator display and resets all variables
    /// </summary>
    private void ClearCalculator()
    {
        currentInput = "";
        currentExpression = "";
        lastWasOperator = false;
        lastWasNumber = false;
        
        UpdateDisplay();
        UpdateEqualsText();
    }
    
    /// <summary>
    /// Called when equals (=) button is pressed
    /// </summary>
    private void OnEqualsPressed()
    {
        Debug.Log("HesapMakinesiController: Equals (=) button pressed");
        
        if (string.IsNullOrEmpty(currentExpression))
        {
            Debug.Log("HesapMakinesiController: No expression to calculate");
            UpdateEqualsText();
            return;
        }
        
        int totalValue = CalculateTotalValue();
        Debug.Log($"HesapMakinesiController: Calculated total value: {totalValue}");
        
        // Update equals button text
        if (equalsButtonText != null)
        {
            equalsButtonText.text = $"= {totalValue}";
            coinDisplayText.text = $"{currentExpression}";
        }
        
        // Here you can add logic to use the totalValue for token spawning
        // For example, call a method to spawn tokens with this value
        OnCalculatorResult(totalValue);
        ForceClose();
    }
    
    /// <summary>
    /// Calculates the total value from the current expression
    /// </summary>
    private int CalculateTotalValue()
    {
        if (string.IsNullOrEmpty(currentExpression))
            return 0;
        
        try
        {
            // Parse the expression: "10 x 2 + 15" -> calculate (10*2) + 15
            string[] parts = currentExpression.Split(' ');
            List<int> numbers = new List<int>();
            List<string> operators = new List<string>();
            
            // First pass: collect all numbers and operators
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                
                if (int.TryParse(part, out int number))
                {
                    numbers.Add(number);
                }
                else if (part == "x" || part == "+")
                {
                    operators.Add(part);
                }
            }
            
            // Second pass: handle multiplication first (operator precedence)
            for (int i = 0; i < operators.Count; i++)
            {
                if (operators[i] == "x")
                {
                    // Multiply the current number with the next number
                    numbers[i] *= numbers[i + 1];
                    numbers.RemoveAt(i + 1);
                    operators.RemoveAt(i);
                    i--; // Recheck this position since we removed an element
                }
            }
            
            // Third pass: handle addition
            int total = numbers[0];
            for (int i = 0; i < operators.Count; i++)
            {
                if (operators[i] == "+")
                {
                    total += numbers[i + 1];
                }
            }
            
            return total;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HesapMakinesiController: Error calculating expression: {e.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Updates the display text with current expression
    /// </summary>
    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            displayText.text = currentExpression;
        }
    }
    
    /// <summary>
    /// Updates the equals button text with calculated result or "geçersiz"
    /// </summary>
    private void UpdateEqualsText()
    {
        if (equalsButtonText == null) return;
        
        if (string.IsNullOrEmpty(currentExpression))
        {
            equalsButtonText.text = "=";
            return;
        }
        
        // Check if the expression ends with an operator (invalid)
        if (currentExpression.Trim().EndsWith("x") || currentExpression.Trim().EndsWith("+"))
        {
            equalsButtonText.text = "= xxx";
            return;
        }
        
        // Try to calculate the result
        try
        {
            int result = CalculateTotalValue();
            equalsButtonText.text = $"= {result}";
        }
        catch
        {
            equalsButtonText.text = "= geçersiz";
        }
    }
    
    /// <summary>
    /// Called when calculator calculation is complete
    /// </summary>
    private void OnCalculatorResult(int totalValue)
    {
        Debug.Log($"HesapMakinesiController: Calculator result ready - Total Value: {totalValue}");
        
        // Parse the expression to get token information
        List<KeseController.TokenData> tokens = ParseExpressionToTokens(currentExpression);
        
        // Send token data to KeseController
        if (tokens.Count > 0)
        {
            SendTokenDataToKeseController(tokens);
        }
    }
    
    /// <summary>
    /// Parses the expression to extract token information
    /// Example: "10 x 2 + 5 x 3" -> [TokenData(10,2), TokenData(5,3)]
    /// </summary>
    private List<KeseController.TokenData> ParseExpressionToTokens(string expression)
    {
        List<KeseController.TokenData> tokens = new List<KeseController.TokenData>();
        
        if (string.IsNullOrEmpty(expression))
            return tokens;
        
        try
        {
            string[] parts = expression.Split(' ');
            int currentValue = 0;
            int currentCount = 1;
            
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                
                if (int.TryParse(part, out int number))
                {
                    if (i == 0 || (i > 0 && parts[i - 1].Trim() == "+"))
                    {
                        // This is a new value (either first number or after +)
                        currentValue = number;
                        currentCount = 1;
                    }
                }
                else if (part == "x")
                {
                    // Next number will be the count for current value
                    if (i + 1 < parts.Length && int.TryParse(parts[i + 1].Trim(), out int count))
                    {
                        currentCount = count;
                        i++; // Skip the count number in next iteration
                    }
                }
                else if (part == "+")
                {
                    // Add the current token data and prepare for next
                    if (currentValue > 0)
                    {
                        tokens.Add(new KeseController.TokenData(currentValue, currentCount));
                    }
                }
            }
            
            // Add the last token data
            if (currentValue > 0)
            {
                tokens.Add(new KeseController.TokenData(currentValue, currentCount));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HesapMakinesiController: Error parsing expression to tokens: {e.Message}");
        }
        
        return tokens;
    }
    
    /// <summary>
    /// Sends token data to KeseController
    /// </summary>
    private void SendTokenDataToKeseController(List<KeseController.TokenData> tokens)
    {
        // Find KeseController in the scene
        KeseController keseController = FindObjectOfType<KeseController>();
        if (keseController != null)
        {
            keseController.SetCoinTokenData(tokens);
            Debug.Log($"HesapMakinesiController: Sent {tokens.Count} token types to KeseController");
        }
        else
        {
            Debug.LogWarning("HesapMakinesiController: KeseController not found in scene!");
        }
    }
    
    /// <summary>
    /// Gets the current calculator result (can be called from other scripts)
    /// </summary>
    public int GetCurrentCalculatorResult()
    {
        return CalculateTotalValue();
    }
    
    /// <summary>
    /// Gets the current expression string (for debugging)
    /// </summary>
    public string GetCurrentExpression()
    {
        return currentExpression;
    }
    
    /// <summary>
    /// Checks if the given GameObject is a calculator button
    /// Called by KeseController to determine if a click should close the calculator
    /// </summary>
    public bool IsPointerOverCalculatorButton(GameObject hitObject)
    {
        if (hitObject == null) return false;
        
        // Check if the hit object is any of our calculator buttons
        foreach (Button button in allCalculatorButtons)
        {
            if (button != null)
            {
                // Check if the hit object is the button itself
                if (button.gameObject == hitObject)
                {
                    return true;
                }
                
                // Check if the hit object is a child of the button
                if (hitObject.transform.IsChildOf(button.transform))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    

    

    
    /// <summary>
    /// Moves the hesap makinesi from starting position to reach point
    /// </summary>
    public void MoveToReachPoint()
    {
        if (isMoving || reachPoint == null) return;
        
        Debug.Log("HesapMakinesiController: Moving to reach point");
        
        // Kill any existing movement sequence
        if (moveSequence != null)
            moveSequence.Kill();
        
        isMoving = true;
        isAtReachPoint = true;
        
        // Create movement sequence
        moveSequence = DOTween.Sequence();
        moveSequence.Append(transform.DOMove(reachPoint.position, moveSpeed).SetEase(moveEase));
        moveSequence.OnComplete(() => {
            isMoving = false;
            Debug.Log("HesapMakinesiController: Reached target position");
        });
    }
    
    /// <summary>
    /// Moves the hesap makinesi from reach point back to starting position
    /// </summary>
    public void MoveToStartingPosition()
    {
        if (isMoving) return;
        
        Debug.Log("HesapMakinesiController: Moving to starting position");
        
        // Kill any existing movement sequence
        if (moveSequence != null)
            moveSequence.Kill();
        
        isMoving = true;
        isAtReachPoint = false;
        
        // Create movement sequence
        moveSequence = DOTween.Sequence();
        moveSequence.Append(transform.DOMove(startingPosition, moveSpeed).SetEase(moveEase));
        moveSequence.OnComplete(() => {
            isMoving = false;
            Debug.Log("HesapMakinesiController: Returned to starting position");
        });
    }
    
    /// <summary>
    /// Public method to check if hesap makinesi is at reach point
    /// </summary>
    public bool IsAtReachPoint()
    {
        return isAtReachPoint;
    }
    
    /// <summary>
    /// Public method to check if hesap makinesi is moving
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
    
    /// <summary>
    /// Public method to manually trigger movement to reach point
    /// </summary>
    public void TriggerMoveToReachPoint()
    {
        MoveToReachPoint();
    }
    
    /// <summary>
    /// Public method to manually trigger movement to starting position
    /// </summary>
    public void TriggerMoveToStartingPosition()
    {
        MoveToStartingPosition();
    }
    
    void OnDestroy()
    {
        // Clean up DOTween sequences
        if (moveSequence != null)
            moveSequence.Kill();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw the reach point in the scene view
        if (reachPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(reachPoint.position, 0.5f);
            
            // Draw line from current position to reach point
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, reachPoint.position);
        }
    }
}

