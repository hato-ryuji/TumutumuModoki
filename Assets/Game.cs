using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour {
    //定数
    private const int START_BALL_NUM = 50;
    private const float DROP_BALL_HEIGHT = 5;
    private const float DROP_BALL_WIDTH = 2.5f;
    private const string OBJNAME_BALL_HEAD = "Ball";
    private const float ADJACENT_RANGE = 1;

    //ボールの処理関係
    public GameObject ballPrefab;
    public Sprite[] ballSprites;
    private GameObject firstBall;
    private List<GameObject> removableBallList;
    private GameObject lastBall;
    private string currentName;

    //タイマー処理周り
    private bool isPlaying = false;
    public GameObject timer;
    private Text timerText;
    private float timeLimit = 60;
    private float countTime = 5;

    //スコア関係の処理
    public GameObject score;
    private Text scoreText;
    private int currentScore;

    // Use this for initialization
    void Start () {
        timerText = timer.GetComponent<Text>();
        scoreText = score.GetComponent<Text>();
        StartCoroutine("CountDown");
        StartCoroutine("DropBalls", START_BALL_NUM);    //シーン開始時のボールの生成
    }

    //ゲーム開始時のカウントダウン
    IEnumerator CountDown() {
        float count = countTime;
        while (count > 0) {
            timerText.text = count.ToString();
            yield return new WaitForSeconds(1);
            count -= 1;
        }
        timerText.text = "Start!";
        isPlaying = true;
        yield return new WaitForSeconds(1);
        StartCoroutine("StartTimer");
    }

    //ゲーム開始処理
    IEnumerator StartTimer() {
        float count = timeLimit;
        while (count > 0) {
            timerText.text = count.ToString();
            yield return new WaitForSeconds(1);
            count -= 1;
        }
        timerText.text = "Finish";
        OnDragEnd();
        isPlaying = false;
    }

    //ボール生成処理
    IEnumerator DropBalls(int count) {
        for (int i = 0; i < count; i++)
        {
            GameObject ball = Instantiate(ballPrefab, 
                new Vector3(-DROP_BALL_WIDTH + DROP_BALL_WIDTH * 2 * Random.value, DROP_BALL_HEIGHT, 0), 
                Quaternion.identity);
            int spriteId = Random.Range(0, 5); //ボールの画像のid(ボールの色)をランダムに設定
            ball.name = OBJNAME_BALL_HEAD + spriteId;
            SpriteRenderer ballTexture = ball.GetComponent<SpriteRenderer>();
            ballTexture.sprite = ballSprites[spriteId];
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Update is called once per frame
    void Update () {
        if (isPlaying) {
            if (Input.GetMouseButton(0) && firstBall == null) {
                //ボールをドラッグしはじめたとき
                OnDragStart();
            }
            else if (Input.GetMouseButtonUp(0)) {
                //ボールをドラッグし終わったとき
                OnDragEnd();
            }
            else if (firstBall != null) {
                //ボールをドラッグしている途中
                OnDragging();
            }
        }
        scoreText.text = "Score:" + currentScore.ToString();
    }

    private void OnDragStart() {
        Collider2D col = GetCurrentHitCollider();
        if (col != null) {
            GameObject colObj = col.gameObject;
            if (colObj.name.IndexOf(OBJNAME_BALL_HEAD) != -1) {
                removableBallList = new List<GameObject>();
                firstBall = colObj;
                currentName = colObj.name;
                PushToList(colObj);
            }
        }
    }

    private void PushToList(GameObject obj) {
        ChangeColor(obj, 0.5f);
        lastBall = obj;
        removableBallList.Add(obj);
        obj.name = "_" + obj.name;
    }

    private Collider2D GetCurrentHitCollider() {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        return hit.collider;
    }

    private void OnDragEnd() {
        if (firstBall != null) {
            int length = removableBallList.Count;
            if (length >= 3) {
                foreach (var removableBall in removableBallList) {
                    Destroy(removableBall);
                }
                currentScore += (CalculateBaseScore(length) + 50 * length);
                StartCoroutine("DropBalls", length);
            } else {
                foreach (var removableBall in removableBallList) {
                    ChangeColor(removableBall, 1.0f);
                    removableBall.name = removableBall.name.Substring(1, 5);
                }
                firstBall = null;
            }
        }
    }

    private void OnDragging() {
        Collider2D col = GetCurrentHitCollider();
        if (col != null) {
            GameObject colObj = col.gameObject;
            if (colObj.name == currentName) {
                float dist= Vector2.Distance(lastBall.transform.position, colObj.transform.position);
                if (dist <= ADJACENT_RANGE) {
                    PushToList(colObj);
                }
            }
        }
    }

    private void ChangeColor(GameObject obj, float transparency) {
        SpriteRenderer ballTexture = obj.GetComponent<SpriteRenderer>();
        ballTexture.color = new Color(1, 1, 1, transparency);
    }

    public void Reset() {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name);
    }

    private int CalculateBaseScore(int num) {
        int tempScore = 50 * num * (num + 1) - 300;
        return tempScore;
    }
}
