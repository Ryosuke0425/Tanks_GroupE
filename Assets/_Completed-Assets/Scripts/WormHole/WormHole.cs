using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace Complete
{
    public class WormHole : MonoBehaviour
    {
        [SerializeField] private GameObject endWormHole;
        [SerializeField] private float warpingTime = 1.0f;
        [SerializeField] private float waitingTime = 0.5f;
        [SerializeField] private float coolingTimer;
        public bool IsCooling { get { return coolingTimer > 0; } }


        // Start is called before the first frame update
        void Start()
        {
            gameObject.GetComponent<Collider>().isTrigger = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (coolingTimer > 0)
            {
                coolingTimer -= Time.deltaTime;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (coolingTimer > 0)
            {
                return;
            }
            TankHealth tankHealth = other.GetComponent<TankHealth>();
            if (tankHealth != null)
            {
                StartCooling();
                endWormHole.GetComponent<WormHole>().StartCooling();
                StartCoroutine(BlinkTankCoroutine(other.transform.Find("TankRenderers").gameObject));

                other.GetComponent<TankHealth>().BeInvincible();
                StartCoroutine(SleepCoroutine(waitingTime, () =>
                {
                    other.GetComponent<Rigidbody>().MovePosition(endWormHole.transform.position);
                }));
            }
        }

        IEnumerator SleepCoroutine(float seconds, UnityAction callback)
        {
            yield return new WaitForSeconds(seconds);
            callback?.Invoke();
        }

        void StartCooling()
        {
            coolingTimer = 2.0f;
        }

        IEnumerator BlinkTankCoroutine(GameObject tankRenderers)
        {
            float elapsedTime = 0f;
            float blinkInterval = 0.2f;
            float lifetime = 2.0f;
            while (elapsedTime < lifetime)
            {
                if (elapsedTime % blinkInterval < blinkInterval / 2)
                {
                    tankRenderers.SetActive(true);
                }
                else
                {
                    tankRenderers.SetActive(false);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            tankRenderers.SetActive(true);
        }
    }
}