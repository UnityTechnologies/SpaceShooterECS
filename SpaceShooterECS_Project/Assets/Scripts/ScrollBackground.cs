using UnityEngine;

public class ScrollBackground : MonoBehaviour
{
	public float speed = -2f;
	public float lowerValue = -20f;
	public float offset = 40;

	void Update()
	{
		transform.Translate(0f, speed * Time.deltaTime, 0f);

		if (transform.position.z <= lowerValue)
		{
			transform.Translate(0f, offset, 0f);
		}
	}

}
