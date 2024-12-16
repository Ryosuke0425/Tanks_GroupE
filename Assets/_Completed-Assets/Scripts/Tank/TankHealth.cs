using UnityEngine;
using UnityEngine.UI;
using System;

namespace Complete
{
    public class TankHealth : MonoBehaviour
    {
        public float m_StartingHealth = 100f;               
        public Slider m_Slider;                             
        public Image m_FillImage;                           
        public Color m_FullHealthColor = Color.green;       
        public Color m_ZeroHealthColor = Color.red;         
        public GameObject m_ExplosionPrefab;                
        public Text m_CurrentHealthDisplay;                 
        public Slider[] hpSlider = new Slider[2];           

        private AudioSource m_ExplosionAudio;               
        private ParticleSystem m_ExplosionParticles;        
        private float m_CurrentHealth;                      
        private bool m_Dead;                                
        private float invincibleTimer;

        public bool IsInvincible { get { return invincibleTimer > 0; } }

        private int m_PlayerNumber; // プレイヤー番号を格納する変数

        private void Awake()
        {
            // タンクのプレイヤー番号を取得（TankMovementから取得）
            TankMovement movement = GetComponent<TankMovement>();
            if (movement != null)
            {
                m_PlayerNumber = movement.m_PlayerNumber;
            }
            else
            {
                Debug.LogError("TankMovementがアタッチされておらず、プレイヤー番号が取得できません。");
            }

            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            SetHealthUI();

            // プレイヤー番号に応じてm_CurrentHealthDisplayを割り当て
            if (m_PlayerNumber == 1)
            {
                m_CurrentHealthDisplay = GameObject.Find("YourHP").GetComponent<Text>();
            }
            else if (m_PlayerNumber == 2)
            {
                m_CurrentHealthDisplay = GameObject.Find("EnemyHP").GetComponent<Text>();
            }

            // HP表示更新
            if (m_CurrentHealthDisplay != null)
            {
                m_CurrentHealthDisplay.text = "HP:" + Mathf.CeilToInt(m_CurrentHealth).ToString();
            }
        }

        private void Update()
        {
            if (invincibleTimer > 0)
            {
                invincibleTimer -= Time.deltaTime;
            }
        }

        public void TakeDamage(float amount)
        {
            m_CurrentHealth -= amount;
            SetHealthUI();

            if (m_CurrentHealthDisplay != null)
            {
                if (m_CurrentHealth <= 0)
                {
                    m_CurrentHealthDisplay.text = "HP:0";
                }
                else
                {
                    m_CurrentHealthDisplay.text = "HP:" + Mathf.CeilToInt(m_CurrentHealth).ToString();
                }
            }

            hpSlider[0].value = Mathf.Max(0, (m_CurrentHealth - 100.0f) / 100.0f);
            hpSlider[1].value = m_CurrentHealth / 100.0f;

            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                OnDeath();
            }
        }

        private void SetHealthUI()
        {
            m_Slider.value = m_CurrentHealth;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }

        // TankHealth内
        private void OnDeath()
        {
            m_Dead = true;
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();
            gameObject.SetActive(false);

            // PVP対応: HP0になったのでラウンド終了をサーバーに通知
            Complete.GameManager gm = FindObjectOfType<Complete.GameManager>();
            if (gm != null)
            {
                gm.NotifyRoundEnd();
            }
        }


        public void BeInvincible()
        {
            invincibleTimer = 2.0f;
        }
    }
}
