﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotesControl : SingletonMonoBehaviour<NotesControl>
{
    // ノーツの最大生成数
    [SerializeField, Tooltip("ノーツの最大生成数"), Range(1, 20), Header("ノーツの基本設定")]
    private int maxNotes = 5;

    // ノーツのプレファブオブジェクト
    [SerializeField, Tooltip("ノーツのPrefab")]
    private GameObject notesPrefab = null;

    [SerializeField, Tooltip("ノーツのSpriteイメージデータ")]
    private Sprite[] notesSprites = null;

    private bool clickFlag = true;    // 入力フラグ

    /// <summary>
    /// ノーツの管理用データベース
    /// </summary>
    private struct NotesDataBase
    {
        // ノーツのオブジェクトプール用の配列
        public NotesView[] NotesObjects;

        // ノーツの呼び出し番号
        private int notesCallCount;
        public int NotesCallCount { set { notesCallCount = value; if (notesCallCount >= NotesObjects.Length || notesCallCount < 0) notesCallCount = 0; } get { return notesCallCount; } }

        // 判定チェック中のノーツ番号
        private int notesCheckCount;
        public int NotesCheckCount { set { notesCheckCount = value; if (notesCheckCount >= NotesObjects.Length || notesCheckCount < 0) notesCheckCount = 0; } get { return notesCheckCount; } }

        // 初期化
        public void ResetDataBase()
        {
            notesCallCount = 0;
            notesCheckCount = 0;
        }
    }
    private NotesDataBase dataBase1;
    private NotesDataBase dataBase2;

    [SerializeField, Tooltip("生成されるノーツのサイズ")]
    private float notesSize = 1.0f;

    [SerializeField, Tooltip("判定ノーツの透明度"), Range(0, 1.0f)]
    private float notesSpriteAlpha = 0.5f;

    [SerializeField, Tooltip("ノーツ判定距離:Perfect"), Range(0.0001f, 0.05f), Header("ノーツの判定域")]
    private float perfectLength = 0.05f;

    [SerializeField, Tooltip("ノーツの判定距離:Good"), Range(0.0001f, 0.05f)]
    private float goodLength = 0.05f;

    [SerializeField, Tooltip("ノーツの判定距離:Bad"), Range(0.0001f, 0.05f)]
    private float badLength = 0.05f;

    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        InputNotesAction(ControllerNum.P1);
        InputNotesAction(ControllerNum.P2);
    }

    /// <summary>
    /// NotesControlの初期化
    /// </summary>
    private void Init()
    {
        dataBase1.ResetDataBase();
        dataBase2.ResetDataBase();
        CreateNotes(ControllerNum.P1);
        CreateNotes(ControllerNum.P2);
    }

    /// <summary>
    /// ノーツの生成
    /// </summary>
    /// <param name="id">プレイヤー番号</param>
    private void CreateNotes(ControllerNum id)
    {
        if (notesPrefab == null)
        {
            Debug.LogError("プレファブが設定されていません");
            return;
        }

        NotesDataBase notesData = id == ControllerNum.P1 ? dataBase1 : dataBase2;

        // NotesView配列の初期化
        notesData.NotesObjects = new NotesView[maxNotes];

        NotesView.Notes singleNotesData;
        NotesView.Notes doubleNotesData;
        NotesView.Notes mashNotesData;

        for (int i = 0; i < maxNotes; i++)
        {
            if(notesData.NotesObjects[i] == null)
            {
                GameObject obj = Instantiate(notesPrefab, gameObject.transform, false);
                for(int j = 0; j < obj.transform.childCount; j++)
                {
                    obj.transform.GetChild(j).gameObject.SetActive(false);
                }
                notesData.NotesObjects[i] = obj.GetComponent<NotesView>();
                singleNotesData = notesData.NotesObjects[i].SingleNotes;
                doubleNotesData = notesData.NotesObjects[i].DoubleNotes;
                mashNotesData = notesData.NotesObjects[i].MashNotes;

                singleNotesData.NotesParentObject = obj.transform.GetChild(0).gameObject;
                singleNotesData.MoveNotesObject = singleNotesData.NotesParentObject.transform.GetChild(1).gameObject;
                singleNotesData.EndNotesObject = singleNotesData.NotesParentObject.transform.GetChild(0).gameObject;
                singleNotesData.MoveNotesSprite1 = singleNotesData.MoveNotesObject.GetComponent<SpriteRenderer>();
                singleNotesData.EndNotesSprite1 = singleNotesData.EndNotesObject.GetComponent<SpriteRenderer>();

                doubleNotesData.NotesParentObject = obj.transform.GetChild(1).gameObject;
                doubleNotesData.MoveNotesObject = doubleNotesData.NotesParentObject.transform.GetChild(1).gameObject;
                doubleNotesData.EndNotesObject = doubleNotesData.NotesParentObject.transform.GetChild(0).gameObject;
                doubleNotesData.MoveNotesSprite1 = doubleNotesData.MoveNotesObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
                doubleNotesData.MoveNotesSprite2 = doubleNotesData.MoveNotesObject.transform.GetChild(1).GetComponent<SpriteRenderer>();
                doubleNotesData.EndNotesSprite1 = doubleNotesData.EndNotesObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
                doubleNotesData.EndNotesSprite2 = doubleNotesData.EndNotesObject.transform.GetChild(1).GetComponent<SpriteRenderer>();

                notesData.NotesObjects[i].SingleNotes = singleNotesData;
                notesData.NotesObjects[i].DoubleNotes = doubleNotesData;
                notesData.NotesObjects[i].MashNotes = mashNotesData;
            }
        }

        _ = id == ControllerNum.P1 ? dataBase1 = notesData : dataBase2 = notesData;
    }

    /// <summary>
    /// ノーツを1回再生する処理
    /// </summary>
    /// <param name="type">再生するノーツのタイプ</param>
    /// <param name="startPos">ノーツの再生開始座標</param>
    /// <param name="endPos">ノーツの判定座標</param>
    /// <param name="id">入力対象のコントローラ番号</param>
    /// <param name="duration">再生開始位置から判定位置まで移動するのにかかる時間[s]</param>
    public void PlayNotesOneShot(NotesType type, Vector3 startPos, Vector3 endPos, ControllerNum id, float duration = 1.0f)
    {
        if (startPos == endPos) { return; }
        NotesDataBase notesData = id == ControllerNum.P1 ? dataBase1 : dataBase2;

        // 呼び出そうとしたノーツがすでに稼働中なら処理を終了
        if (notesData.NotesObjects[notesData.NotesCallCount].SingleNotes.NotesParentObject.activeSelf == true || notesData.NotesObjects[notesData.NotesCallCount].DoubleNotes.NotesParentObject.activeSelf == true) { return; }

        // ノーツにデータをセットして再生する
        notesData.NotesObjects[notesData.NotesCallCount].Mode = NotesMode.Single;
        notesData.NotesObjects[notesData.NotesCallCount].SingleNotesData(type, startPos, endPos, duration, perfectLength, goodLength, badLength, new Vector3(notesSize, notesSize, notesSize), notesSprites[(int)type], notesSpriteAlpha);
        notesData.NotesCallCount++;

        _ = id == ControllerNum.P1 ? dataBase1.NotesCallCount = notesData.NotesCallCount : dataBase2.NotesCallCount = notesData.NotesCallCount;
    }

    /// <summary>
    /// ダブルノーツを1回再生する
    /// </summary>
    /// <param name="type1">再生するノーツのタイプ１</param>
    /// <param name="type2">再生するノーツのタイプ２</param>
    /// <param name="startPos">ノーツの再生開始座標</param>
    /// <param name="endPos">ノーツの判定座標</param>
    /// <param name="id">入力対象のコントローラ番号</param>
    /// <param name="duration">再生開始位置から判定位置まで移動するのにかかる時間[s]</param>
    public void PlayDoubleNotesOneShot(NotesType type1, NotesType type2, Vector3 startPos, Vector3 endPos, ControllerNum id, float duration = 1.0f)
    {
        if(startPos == endPos) { return; }
        NotesDataBase notesData = id == ControllerNum.P1 ? dataBase1 : dataBase2;

        // 呼び出そうとしたノーツがすでに稼働中なら処理を終了
        if (notesData.NotesObjects[notesData.NotesCallCount].SingleNotes.NotesParentObject.activeSelf == true || notesData.NotesObjects[notesData.NotesCallCount].DoubleNotes.NotesParentObject.activeSelf == true) { return; }

        // ノーツにデータをセットして再生する
        notesData.NotesObjects[notesData.NotesCallCount].Mode = NotesMode.Double;
        notesData.NotesObjects[notesData.NotesCallCount].DoubleNotesData(type1, type2, startPos, endPos, duration, perfectLength, goodLength, badLength, new Vector3(notesSize, notesSize, notesSize), notesSprites[(int)type1], notesSprites[(int)type2], notesSpriteAlpha);
        notesData.NotesCallCount++;

        _ = id == ControllerNum.P1 ? dataBase1.NotesCallCount = notesData.NotesCallCount : dataBase2.NotesCallCount = notesData.NotesCallCount;
    }

    /// <summary>
    /// 全てのノーツの再生を止める
    /// </summary>
    public void StopNotes()
    {
        for(int i = 0; i < maxNotes; i++)
        {
            if(dataBase1.NotesObjects[i].gameObject.activeSelf == true)
            {
                dataBase1.NotesObjects[i].ResetNotes();
            }

            if(dataBase2.NotesObjects[i].gameObject.activeSelf == true)
            {
                dataBase2.NotesObjects[i].ResetNotes();
            }
        }
        dataBase1.NotesCallCount = 0;
        dataBase1.NotesCheckCount = 0;
        dataBase2.NotesCallCount = 0;
        dataBase2.NotesCheckCount = 0;
    }

    /// <summary>
    /// ノーツの入力処理
    /// </summary>
    /// <param name="id">コントローラ番号</param>
    private void InputNotesAction(ControllerNum id)
    {
        NotesView nowNotes;
        int nextNotesNum;
        NotesView nextNotes;

        if(id == ControllerNum.P1)
        {
            nowNotes = dataBase1.NotesObjects[dataBase1.NotesCheckCount];
            nextNotesNum = dataBase1.NotesCheckCount + 1 >= dataBase1.NotesObjects.Length ? 0 : dataBase1.NotesCheckCount + 1;
            nextNotes = dataBase1.NotesObjects[nextNotesNum];
        }
        else
        {
            nowNotes = dataBase2.NotesObjects[dataBase2.NotesCheckCount];
            nextNotesNum = dataBase2.NotesCheckCount + 1 >= dataBase2.NotesObjects.Length ? 0 : dataBase2.NotesCheckCount + 1;
            nextNotes = dataBase2.NotesObjects[nextNotesNum];
        }

        if(nowNotes.Mode == NotesMode.Single)
        {
            if(nowNotes.SingleNotes.NotesParentObject.activeSelf == false) { return; }
        }
        else if(nowNotes.Mode == NotesMode.Double)
        {
            if(nowNotes.DoubleNotes.NotesParentObject.activeSelf == false) { return; }
        }

        if(nowNotes.NotesClickFlag == true)
        {
            if(nowNotes.Mode == NotesMode.Single)    // ノーツがシングルモードの時の入力
            {
                if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == true && clickFlag == true)
                {
                    NotesCheck(nowNotes, NotesType.CircleKey, id);
                    clickFlag = false;
                    return;
                }

                if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true && clickFlag == true)
                {
                    NotesCheck(nowNotes, NotesType.CrossKey, id);
                    clickFlag = false;
                    return;
                }

                if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true && clickFlag == true)
                {
                    NotesCheck(nowNotes, NotesType.TriangleKey, id);
                    clickFlag = false;
                    return;
                }

                if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == true && clickFlag == true)
                {
                    NotesCheck(nowNotes, NotesType.UpArrow, id);
                    clickFlag = false;
                    return;
                }

                if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == true && clickFlag == true)
                {
                    NotesCheck(nowNotes, NotesType.DownArrow, id);
                    clickFlag = false;
                    return;
                }

                if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == true && clickFlag == true)
                {
                    NotesCheck(nowNotes, NotesType.LeftArrow, id);
                    clickFlag = false;
                    return;
                }
            }
            else if (nowNotes.Mode == NotesMode.Double)    // 　ノーツがダブルモードの時の入力
            {
                if((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 0, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 1, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 2, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 3, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 4, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 5, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 6, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 6, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 7, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 8, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 9, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 10, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 11, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 12, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 13, id);
                    clickFlag = false;
                    return;
                }

                if ((GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == true && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == true) && clickFlag == true)
                {
                    NotesCheck(nowNotes, 14, id);
                    clickFlag = false;
                    return;
                }
            }
            else
            {

            }

            // キー入力が検知されなかったら入力を許可
            if (GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Circle) == false && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Cross) == false && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Triangle) == false && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Up) == false && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Down) == false && GamePadControl.Instance.GetDS4Key(id, DS4AllKeyType.Left) == false && clickFlag == false)
            {
                clickFlag = true;
                return;
            }
        }

        if(nowNotes.Mode == NotesMode.Single)
        {
            if (nowNotes.NotesClickFlag == false || (nextNotes.SingleNotes.NotesParentObject.activeSelf == true && Mathf.Abs(0.5f - nowNotes.NotesRate) > Mathf.Abs(0.5f - nextNotes.NotesRate)))
            {
                NotesResult(0, 0, id);
                return;
            }
        }
        else if (nowNotes.Mode == NotesMode.Double)
        {
            if (nowNotes.NotesClickFlag == false || (nextNotes.DoubleNotes.NotesParentObject.activeSelf == true && Mathf.Abs(0.5f - nowNotes.NotesRate) > Mathf.Abs(0.5f - nextNotes.NotesRate)))
            {
                NotesResult(0, 0, id);
                return;
            }
        }
    }

    /// <summary>
    /// ノーツの判定処理
    /// </summary>
    /// <param name="view">判定をチェックしたいノーツ</param>
    /// <param name="type">判定ID</param>
    /// <param name="id">プレイヤー番号</param>
    private void NotesCheck(NotesView view,  NotesType type, ControllerNum id)
    {
        // ノーツの進行率をチェック
        var rate = view.NotesRate;
        if(rate < view.MinBad) { return; }

        // ノーツを非表示
        view.ResetNotes();

        // ノーツの判定をチェック
        int result;
        int score;
        if (type == view.NotesType1)
        {
            if(rate >= view.MinPerfect && rate <= view.MaxPerfect)
            {
                result = 2;
                score = 800;
            }
            else if((rate >= view.MinGood && rate < view.MinPerfect) || (rate <= view.MaxGood && rate > view.MaxPerfect))
            {
                result = 1;
                score = 400;
            }
            else
            {
                result = 0;
                score = 0;
            }
        }
        else
        {
            result = 0;
            score = 0;
        }

        // 結果を算出
        NotesResult(result, score, id);
    }

    /// <summary>
    /// ノーツの判定処理(ダブル用)
    /// </summary>
    /// <param name="view">判定をチェックしたいノーツ</param>
    /// <param name="type">入力されたノーツタイプID</param>
    /// <param name="id">プレイヤー番号</param>
    private void NotesCheck(NotesView view, int type, ControllerNum id)
    {
        // ノーツの進行率をチェック
        var rate = view.NotesRate;
        if (rate < view.MinBad) { return; }

        // ノーツを非表示
        view.ResetNotes();

        // ノーツの判定をチェック
        int result;
        int score;
        if (type == view.NotesTypeNum)
        {
            if (rate >= view.MinPerfect && rate <= view.MaxPerfect)
            {
                result = 2;
                score = 800;
            }
            else if ((rate >= view.MinGood && rate < view.MinPerfect) || (rate <= view.MaxGood && rate > view.MaxPerfect))
            {
                result = 1;
                score = 400;
            }
            else
            {
                result = 0;
                score = 0;
            }
        }
        else
        {
            result = 0;
            score = 0;
        }

        // 結果を算出
        NotesResult(result, score, id);
    }

    /// <summary>
    /// ノーツの判定結果
    /// </summary>
    /// <param name="result">ノーツの判定値</param>
    /// <param name="score">獲得スコア</param>
    /// <param name="id">プレイヤー番号</param>
    private void NotesResult(int result, int score, ControllerNum id)
    {
        NotesDataBase notesData = id == ControllerNum.P1 ? dataBase1 : dataBase2;

        switch (result)
        {
            case 0:
                GameData.Instance.PlusNotesResult(id, 0);
                break;
            case 1:
                GameData.Instance.PlusNotesResult(id, 1);
                break;
            case 2:
                GameData.Instance.PlusNotesResult(id, 2);
                break;
            default:
                return;
        }

        notesData.NotesCheckCount++;
        GameData.Instance.PlusTotalScore(id, score);

        _ = id == ControllerNum.P1 ? dataBase1 = notesData : dataBase2 = notesData;
    }
}
