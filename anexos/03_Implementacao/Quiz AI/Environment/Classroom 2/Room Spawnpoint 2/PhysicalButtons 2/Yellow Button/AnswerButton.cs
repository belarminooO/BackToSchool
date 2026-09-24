using System.Collections;
using UnityEngine;

public class AnswerButton : MonoBehaviour, IInteractable
{
    public int answerIndex; 
    public AnswerButtonManager manager;
    public float pressDistance = 0.1f;
    public float animationSpeed = 2f;

    private Vector3 originalLocalPosition;
    private Vector3 pressedLocalPosition;
    private Coroutine animationCoroutine;
    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        pressedLocalPosition = originalLocalPosition - new Vector3(0, pressDistance, 0);
    }

    public void Interact(GameObject interactor)
    {
        if (manager != null)
        {
            GeneratedQuizSfx.PlayButtonPress();
            manager.SubmitAnswer(this, interactor);
        }
        else
        {

        }
    }

    public void PressDown()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateButton(pressedLocalPosition));
    }

    public void ReleaseUp()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateButton(originalLocalPosition));
    }

    private IEnumerator AnimateButton(Vector3 targetPosition)
    {
        while (transform.localPosition != targetPosition)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, animationSpeed * Time.deltaTime);
            yield return null;
        }
    }

}
