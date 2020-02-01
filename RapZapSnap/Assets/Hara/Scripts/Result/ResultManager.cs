﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultManager : MonoBehaviour
{
    [SerializeField, Header("シーン内のキャンバススケーラー")] private CanvasScaler scaler = null;

    [System.Serializable]
    private struct ResultDataBase
    {
        [Header("リザルトに表示するキャラクターの画像リスト")] public Sprite[] CharacterSprites;
        [Header("リザルトに表示するキャラクター画像のオブジェクト")] public Image CharacterImage;
        [Header("トータルスコアのオブジェクト")] public GameObject TotalScoreObj;
        [Header("トータルスコアのテキスト")] public Text TotalScoreText;
        [Header("Rapのスコアテキスト")] public Text RapText;
        [Header("Zapのスコアテキスト")] public Text ZapText;
        [Header("Snapのスコアテキスト")] public Text SnapText;
        [Header("Winの画像データ")] public Sprite WinSprite;

        /// <summary>
        /// オブジェクトの初期化
        /// </summary>
        /// <param name="startPos">初期座標</param>
        /// <param name="index">データ番号</param>
        public void Init(Vector3 startPos, int index)
        {
            CharacterImage.transform.localPosition = startPos;
            CharacterImage.sprite = CharacterSprites[index];
            TotalScoreObj.transform.localPosition = startPos;
            TotalScoreText.text = "0";
            RapText.text = "";
            ZapText.text = "";
            SnapText.text = "";
        }

        /// <summary>
        /// リザルトを表示
        /// </summary>
        /// <param name="endPos">終点座標</param>
        /// <param name="rap">rapのスコア値</param>
        /// <param name="zap">zapのスコア値</param>
        /// <param name="snap">snapのスコア値</param>
        /// <param name="total">totalのスコア値</param>
        public void OutResult(Vector3 endPos, int rap, int zap, int snap, int total)
        {
            CharacterImage.transform.localPosition = endPos;
            TotalScoreObj.transform.localPosition = endPos;
            RapText.text = rap.ToString();
            ZapText.text = zap.ToString();
            SnapText.text = snap.ToString();
            TotalScoreText.text = total.ToString();
        }
    }

    [SerializeField, Header("1P")] private ResultDataBase player1 = new ResultDataBase();
    [SerializeField, Header("2P")] private ResultDataBase player2 = new ResultDataBase();

    [SerializeField, Header("スコアボードオブジェクト")] private GameObject scoreBoardObject = null;
    [SerializeField, Header("Winオブジェクト")] private Image winImageObj = null;

    [SerializeField, Header("リザルトシーン用のSE")] private AudioClip[] resultSE = null;
    private AudioSource resultAudio = null;

    private struct MoveObjPos
    {
        public Vector3 StartPos;
        public Vector3 EndPos;
    }

    private MoveObjPos objectMove_1 = new MoveObjPos();
    private MoveObjPos objectMove_2 = new MoveObjPos();
    private MoveObjPos scoreBoardMove;

    private float time = 0;
    private float span = 0;
    private float diff = 0;
    private int step = 0;
    private bool stepEndFlag = false;
    private bool actionFlag = false;
    [SerializeField, Tooltip("シーン番号"), Header("タイトルシーン")] private int sceneNum = 0;

    // Start is called before the first frame update
    void Start()
    {
        ResultInit();
    }

    // Update is called once per frame
    void Update()
    {
        ResultAction();
    }

    /// <summary>
    /// リザルトシーンの初期化を行う
    /// </summary>
    private void ResultInit()
    {
        // 1P用の座標設定
        objectMove_1.EndPos = new Vector3(0, 0, 0);
        objectMove_1.StartPos = Vector3.left * scaler.referenceResolution.x + objectMove_1.EndPos;

        // 2P用の座標設定
        objectMove_2.EndPos = new Vector3(0, 0, 0);
        objectMove_2.StartPos = Vector3.right * scaler.referenceResolution.x + objectMove_1.EndPos;

        // ScoreBoardの初期化
        scoreBoardMove.EndPos = new Vector3(0, 0, 0);
        scoreBoardMove.StartPos = Vector3.up * scaler.referenceResolution.y + scoreBoardMove.EndPos;

        resultAudio = GetComponent<AudioSource>();

        step = 0;

        actionFlag = true;
    }

    /// <summary>
    /// リザルトを表示する処理を実行
    /// </summary>
    private void ResultAction()
    {
        if (actionFlag == false) { return; }

        switch (step)
        {
            case 0:
                // オブジェクトを初期値に配置及び初期化
                player1.Init(objectMove_1.StartPos, (int)GameData.Instance.GetCharacterData(ControllerNum.P1));
                player2.Init(objectMove_2.StartPos, (int)GameData.Instance.GetCharacterData(ControllerNum.P2));

                scoreBoardObject.transform.localPosition = scoreBoardMove.StartPos;

                winImageObj.gameObject.SetActive(false);

                stepEndFlag = true;
                break;
            case 1:
                // 1Pのキャラクターをスライドイン
                SlideInObject(player1.CharacterImage.gameObject, objectMove_1.StartPos, objectMove_1.EndPos, 0.75f);
                break;
            case 2:
                // 2Pのキャラクターをスライドイン
                SlideInObject(player2.CharacterImage.gameObject, objectMove_2.StartPos, objectMove_2.EndPos, 0.75f);
                break;
            case 3:
                // スコアボードをスライドイン
                SlideInObject(scoreBoardObject, scoreBoardMove.StartPos, scoreBoardMove.EndPos, 0.75f);
                break;
            case 4:
                // Rapのスコアを表示
                StartScoreAnimation(player1.RapText, player2.RapText, GameData.Instance.GetRapScore(ControllerNum.P1), GameData.Instance.GetRapScore(ControllerNum.P2), 0.75f);
                break;
            case 5:
                // Zapのスコアを表示
                StartScoreAnimation(player1.ZapText, player2.ZapText, GameData.Instance.GetZapScore(ControllerNum.P1), GameData.Instance.GetZapScore(ControllerNum.P2), 0.75f);
                break;
            case 6:
                // Snapのスコアを表示
                StartScoreAnimation(player1.SnapText, player2.SnapText, GameData.Instance.GetSnapScore(ControllerNum.P1), GameData.Instance.GetSnapScore(ControllerNum.P2), 0.75f);
                break;
            case 7:
                // トータルスコアボードをスライドイン
                span = 0.5f;
                diff = time / span;
                player1.TotalScoreObj.transform.localPosition = Vector3.Lerp(objectMove_1.StartPos, objectMove_1.EndPos, diff);
                player2.TotalScoreObj.transform.localPosition = Vector3.Lerp(objectMove_2.StartPos, objectMove_2.EndPos, diff);
                time += Time.deltaTime;
                if (time >= span)
                {
                    player1.TotalScoreObj.transform.localPosition = objectMove_1.EndPos;
                    player2.TotalScoreObj.transform.localPosition = objectMove_2.EndPos;
                    stepEndFlag = true;
                }
                break;
            case 8:
                // トータルスコアを表示
                span = 1.0f;
                diff = time / span;
                player1.TotalScoreText.text = ((int)(GameData.Instance.GetTotalScore(ControllerNum.P1) * diff)).ToString();
                player2.TotalScoreText.text = ((int)(GameData.Instance.GetTotalScore(ControllerNum.P2) * diff)).ToString();
                time += Time.deltaTime;
                if (time >= span)
                {
                    player1.TotalScoreText.text = GameData.Instance.GetTotalScore(ControllerNum.P1).ToString();
                    player2.TotalScoreText.text = GameData.Instance.GetTotalScore(ControllerNum.P2).ToString();
                    stepEndFlag = true;
                }
                break;
            case 9:
                // Winオブジェクトを表示
                span = 0.5f;
                time += Time.deltaTime;
                if (time >= span)
                {
                    winImageObj.sprite = GameData.Instance.GetWinnerPlayer() == ControllerNum.P1 ? player1.WinSprite : player2.WinSprite;
                    winImageObj.gameObject.SetActive(true);
                    if (resultSE[0] != null) { resultAudio.PlayOneShot(resultSE[0]); }
                    stepEndFlag = true;
                }
                break;
            default:
                if (resultAudio.isPlaying == true) { return; }
                if ((GamePadControl.Instance.GetKeyDown_1.Circle == true || GamePadControl.Instance.GetKeyDown_2.Circle == true) && actionFlag == true)
                {
                    step = 0;
                    actionFlag = false;
                    if (resultSE[1] != null)
                    {
                        resultAudio.PlayOneShot(resultSE[1]);
                    }
                    SceneControl.Instance.LoadScene(sceneNum);
                }
                return;
        }

        if (stepEndFlag == true)
        {
            step++;
            stepEndFlag = false;
            time = 0;
        }

        // どちらかのプレイヤーが決定キーを入力したらアニメーションをスキップ
        if ((GamePadControl.Instance.GetKeyDown_1.Circle == true || GamePadControl.Instance.GetKeyDown_2.Circle == true) && actionFlag == true)
        {
            step = -1;
            stepEndFlag = false;
            time = 0;

            player1.OutResult(objectMove_1.EndPos, GameData.Instance.GetRapScore(ControllerNum.P1), GameData.Instance.GetZapScore(ControllerNum.P1), GameData.Instance.GetSnapScore(ControllerNum.P1), GameData.Instance.GetTotalScore(ControllerNum.P1));
            player2.OutResult(objectMove_2.EndPos, GameData.Instance.GetRapScore(ControllerNum.P2), GameData.Instance.GetZapScore(ControllerNum.P2), GameData.Instance.GetSnapScore(ControllerNum.P2), GameData.Instance.GetTotalScore(ControllerNum.P2));
            scoreBoardObject.transform.localPosition = scoreBoardMove.EndPos;
            winImageObj.sprite = GameData.Instance.GetWinnerPlayer() == ControllerNum.P1 ? player1.WinSprite : player2.WinSprite;
            winImageObj.gameObject.SetActive(true);

            if (resultSE[0] != null) { resultAudio.PlayOneShot(resultSE[0]); }
        }
    }

    /// <summary>
    /// オブジェクトをスライドインさせる処理
    /// </summary>
    /// <param name="imageObject">動かしたいオブジェクト</param>
    /// <param name="start">開始座標</param>
    /// <param name="end">終了座標</param>
    /// <param name="duration">移動にかかる時間</param>
    private void SlideInObject(GameObject imageObject, Vector3 start, Vector3 end, float duration)
    {
        diff = time / duration;
        imageObject.transform.localPosition = Vector3.Lerp(start, end, this.diff);
        time += Time.deltaTime;
        if (time >= duration)
        {
            imageObject.transform.localPosition = end;
            stepEndFlag = true;
        }
    }

    /// <summary>
    /// スコア表示用のアニメーション処理
    /// </summary>
    /// <param name="p1">プレイヤー1のスコアテキスト</param>
    /// <param name="p2">プレイヤー2のスコアテキスト</param>
    /// <param name="score1">表示するプレイヤー1のスコア値</param>
    /// <param name="score2">表示するプレイヤー2のスコア値</param>
    /// <param name="duration">アニメーションの時間</param>
    private void StartScoreAnimation(Text p1, Text p2, int score1, int score2, float duration)
    {
        int i = 11 * Random.Range(1, 10);
        p1.text = i.ToString();
        p2.text = i.ToString();
        time += Time.deltaTime;
        if (time >= duration)
        {
            p1.text = score1.ToString();
            p2.text = score2.ToString();
            stepEndFlag = true;
        }
    }
}
