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
    private const string OBJNAME_BOMB = "Bomb";
    private const float ADJACENT_RANGE = 1;
    private const float BOMB_EFFECT_RANGE = 1.5f;

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
    
    private bool isDragging = false;
    public GameObject bombPrefab;

    public GameObject fever;

    private float feverCount = 0.0f; //フィーバーゲージの値
    private bool isfever = false; //フィーバー中かどうか
    private float MINfever = 0; //ゲージが0の時のゲージのx座標
    private float MAXfeverCOUNT = 25.0f; //フィーバーゲージの最大値


    // Use this for initialization
    void Start () {
        timerText = timer.GetComponent<Text>();
        scoreText = score.GetComponent<Text>();

        MINfever = fever.GetComponent<RectTransform>().position.x; //ゲージの初期座標を取得
        Debug.Log("MINfever:" + MINfever);

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
        StartCoroutine("Updatefever");  //ゲージを減らす関数を呼ぶ
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
        if (Input.GetMouseButtonDown(0)) {
            OnClick();
        }
        if (isPlaying) {
            //ゲージの位置を更新 C#では直接x座標の更新ができないのでposition要素を変数として取り出す必要がある。
            Vector3 pos = fever.GetComponent<RectTransform>().position;
            pos.x = MINfever + feverCount * MINfever / MAXfeverCOUNT;
            Debug.Log("pos.x" + pos.x);
            fever.GetComponent<RectTransform>().position = pos;

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

    private void Addfever(int num) {
        if (!isfever) {
            feverCount += num;
            Debug.Log("feverCount:" + feverCount);
            if (feverCount > MAXfeverCOUNT) {
                Debug.Log("feverTime 突入");
                feverCount = MAXfeverCOUNT;
                isfever = true;
                timeLimit += 5;//残り時間を5秒増やす
            }
        }
    }

    IEnumerator Updatefever() {
        while (isPlaying) {
            yield return new WaitForSeconds(0.05f);
            if (!isfever) {
                feverCount -= 1.0f / 80.0f;
                if (feverCount < 0) feverCount = 0;
            }
            else {
                //フィーバー中は素早く減らす
                feverCount -= MAXfeverCOUNT / 8.0f / 20.0f;
                if (feverCount < 0) {
                    feverCount = 0;
                    isfever = false;
                }
            }
        }
    }

    private void ClearRemovables(int mode) {
        if (removableBallList != null) {
            var length = removableBallList.Count;
            for (var i = 0; i < length; i++) {
                if (i == length - 1 && mode == 0 && length > 6) {
                    //ボールが7個以上つながっているとき（ボムで消した時には更にボムが生成されないようにする）
                    GameObject bomb = Instantiate(bombPrefab);
                    GameObject obj = removableBallList[i];
                    bomb.transform.position = obj.transform.position;
                    bomb.name = OBJNAME_BOMB;
                }
                Destroy(removableBallList[i]);
            }
            int mult = 1;
            if (isfever) mult = 3; //フィーバータイムに値を3倍に
            currentScore += ((CalculateBaseScore(length) + 50 * length)) * mult;
            Addfever(length); //フィーバーゲージの値を追加
            isDragging = false; //ドラッグおわり
            StartCoroutine("DropBalls", length);
        }
    }

    private void OnDragStart() {
        Collider2D col = GetCurrentHitCollider();
        if (col != null) {
            GameObject colObj = col.gameObject;
            if (colObj.name.IndexOf(OBJNAME_BALL_HEAD) != -1) {
                removableBallList = new List<GameObject>();
                isDragging = true;
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

    private void OnClick() {
        Collider2D col = GetCurrentHitCollider();  // クリックしたオブジェクトを取得
        if (col != null) {
            GameObject colObj = col.gameObject;
            if (colObj.name == OBJNAME_BOMB && isPlaying && !isDragging) {
                //クリックしたオブジェクトがボム　かつ　プレイ中　かつ　ドラッグ中でない
                ClearBomb(colObj);  //ボムクリック時の処理を実行
            }
        }
    }

    private void ClearBomb(GameObject colObj) {
        GameObject[] balls = GameObject.FindGameObjectsWithTag(OBJNAME_BALL_HEAD);  //全てのボールを取得
        removableBallList = new List<GameObject>(); //消去するボールのリストを初期化
        foreach (var ball in balls) {
            float dist = Vector2.Distance(colObj.transform.position, ball.transform.position); //ボムと各ボールの距離を計算
            if (dist < BOMB_EFFECT_RANGE) removableBallList.Add(ball); //距離が一定値以下なら消去するリストに追加
        }
        ClearRemovables(1); //ボールを消す。ボムにより消したときは引数に1を入れる。
        Destroy(colObj);
    }

    private void OnDragEnd() {
        if (firstBall != null) {
            int length = removableBallList.Count;
            if (length >= 3) {
                ClearRemovables(0);
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
