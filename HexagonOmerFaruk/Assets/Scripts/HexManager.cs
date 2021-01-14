using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexManager : MonoBehaviour
{
    public GameObject hex;
    public GameObject bomb;
    GameObject[,] hexagons;
    Transform hexSelect;

    float yPos = 0;
    float xPos = 0;

    [SerializeField]
    int xGrid = 8;
    [SerializeField]
    int yGrid = 9;

    [SerializeField]
    Color[] colors;

    Touch touch;
    Vector2 beganPos;
    Vector2 direction;
    bool rotate;
    bool rotateStart;
    bool rotateRight;
    float hexSelectRotation;

    bool hexFall;

    public Text scoreText;
    int score = 0;

    bool gameOver;
    public GameObject GameOverUI;

    void Start()
    {
        hexSelect = transform.GetChild(0);
        hexagons = new GameObject[8,9];
        Vector2 hexPos = new Vector2();

        //Grid boyunca altıgenler yerleştirilir.
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                hexPos.x = xPos;
                hexPos.y = yPos;
                GameObject hexagon = Instantiate(hex, hexPos, Quaternion.Euler(0, 180, 90), transform);
                hexagon.GetComponent<Renderer>().material.color = colors[Random.Range(0, colors.Length)];
                hexagon.GetComponent<Hexagon>().x = x;
                hexagon.GetComponent<Hexagon>().y = y;

                hexagons[x,y] = hexagon;
                yPos += 1;
            }
            if (x % 2 == 0)
                yPos = -0.5f;
            else
                yPos = 0;

            xPos += 0.865f;
        }

        StartCoroutine(CheckItAll());
    }

    void Update()
    {
        if (!gameOver)
        {

            //Boş olan yerlere üstteki altıgenler düşer.
            if (hexFall)
            {
                HexFalls();
            }

            //Seçilenleri döndürür.
            else if (rotateStart)
            {
                RotateHexSelect();
            }

            //Üç adet altıgen seçer.
            else if (Input.touchCount > 0)
            {
                SelectHex();
            }
        }
    }

    void RotateHexSelect()
    {
        if (rotateRight)
        {
            hexSelect.rotation = Quaternion.Slerp(hexSelect.rotation, Quaternion.Euler(0, 0, hexSelectRotation - 120), Time.deltaTime * 8);
        }
        else
        {
            hexSelect.rotation = Quaternion.Slerp(hexSelect.rotation, Quaternion.Euler(0, 0, hexSelectRotation + 120), Time.deltaTime * 8);
        }
    }

    private IEnumerator RotateFinish()
    {
        yield return new WaitForSeconds(1);
        //Dönen altıgenlerin koordinasyonunu güncelle;
        CoordinationUpdate();
        //Debug.Log("bekle");
        //yield return new WaitForSeconds(5);
        //Etraftaki altıgenlerin rengini kontrol et. (İlk döndürme yapıldı.)
        bool success = false;
        for (int i = 0; i < 3; i++)
        {
            if (CheckNearbyHexs(hexSelect.GetChild(i).gameObject, false, Color.clear, 0, 0))
            {
                success = true;
            }
        }
        if (success)
        {
            //Altıgenleri patlatt...
            HexCrashDestroy();
        }
        else
        {
            hexSelectRotation = hexSelect.rotation.eulerAngles.z;
            yield return new WaitForSeconds(1);
            CoordinationUpdate();
            //Debug.Log("bekle");
            //yield return new WaitForSeconds(5);
            //Etraftaki altıgenlerin rengini kontrol et. (İkinci döndürme yapıldı.)
            for (int i = 0; i < 3; i++)
            {
                if (CheckNearbyHexs(hexSelect.GetChild(i).gameObject, false, Color.clear, 0, 0))
                {
                    success = true;
                }
            }

            if (success)
            {
                //Altıgenleri patlatt...
                HexCrashDestroy();
            }
            else
            {
                hexSelectRotation = hexSelect.rotation.eulerAngles.z;
                yield return new WaitForSeconds(1);
                CoordinationUpdate();
            }
        }

        //Bomba countdown düşür...
        StartCoroutine(BombCountDown());

        rotateStart = false;
    }

    private IEnumerator BombCountDown()
    {
        yield return new WaitForSeconds(2f);
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                if (hexagons[x, y].GetComponent<Hexagon>().isBomb)
                {
                    int countDown = int.Parse(hexagons[x, y].transform.GetChild(0).GetComponent<TextMesh>().text);
                    hexagons[x, y].transform.GetChild(0).GetComponent<TextMesh>().text = (countDown - 1).ToString();

                    if(countDown == 1)
                    {
                        gameOver = true;
                        GameOverUI.SetActive(true);
                        GameOverUI.transform.GetChild(3).gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    void LeaveHexSelect()
    {
        if (hexSelect.childCount > 0)
        {
            for (int i = hexSelect.childCount; i > 0; i--)
            {
                hexSelect.GetChild(0).parent = hexSelect.parent;
            }

            hexSelect.position = new Vector3(-3, 0, -0.1f);
        }
    }

    void HexCrashDestroy()
    {
        //İşaretlenen altıgenleri yok eder.
        int newScore = int.Parse(scoreText.text);
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                if (hexagons[x, y].GetComponent<Hexagon>().destroy)
                {
                    Destroy(hexagons[x, y]);
                    newScore += 5;
                }
            }
        }
        scoreText.text = newScore.ToString();
        //Altıgenler patlayınca yukarıdaki altıgenler düşmeye başlar.
        hexFall = true;
        StartCoroutine(HexFallFinish());
        LeaveHexSelect();
    }

    private IEnumerator HexFallFinish()
    {
        //Yukarıdaki altıgenler düşünce yeni altıgenler üretilir.
        yield return new WaitForSeconds(2f);
        hexFall = false;
        AfterFall();
    }

    void AfterFall()
    {
        //Yeni altıgenlerin üretilmesi:
        int fallCount = 0;
        bool create = false;
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                while (y < yGrid && hexagons[x, y] == null)
                {
                    fallCount++;
                    y++;
                    create = true;
                }
                if (create)
                {
                    int fallMore = 0;
                    for (int fallingY = y; fallingY < yGrid; fallingY++)
                    {
                        if (hexagons[x, fallingY] == null)
                        {
                            fallMore++;
                            continue;
                        }
                        hexagons[x, fallingY - fallCount - fallMore] = hexagons[x, fallingY];
                    }

                    //fallCount kadar altıgen üret...
                    for (int a = 0; a < fallCount + fallMore; a++)
                    {
                        GameObject hexagon;
                        score += 5;
                        if(score % 1000 == 0)
                        {
                            if (x % 2 == 0)
                                hexagon = Instantiate(bomb, new Vector2(x * 0.865f, yGrid - a - 1), Quaternion.Euler(0, 180, 90), transform);
                            else
                                hexagon = Instantiate(bomb, new Vector2(x * 0.865f, yGrid - a - 1.5f), Quaternion.Euler(0, 180, 90), transform);

                            hexagon.GetComponent<Hexagon>().isBomb = true;
                        }
                        else
                        {
                            if (x % 2 == 0)
                                hexagon = Instantiate(hex, new Vector2(x * 0.865f, yGrid - a - 1), Quaternion.Euler(0, 180, 90), transform);
                            else
                                hexagon = Instantiate(hex, new Vector2(x * 0.865f, yGrid - a - 1.5f), Quaternion.Euler(0, 180, 90), transform);
                        }


                        hexagon.GetComponent<Renderer>().material.color = colors[Random.Range(0, colors.Length)];

                        hexagon.GetComponent<Hexagon>().x = x;
                        hexagon.GetComponent<Hexagon>().y = yGrid - a - 1;
                        hexagons[x, yGrid - a - 1] = hexagon;
                    }

                    y = yGrid;
                }
            }
            fallCount = 0;
            create = false;
        }
        //Yeni altıgenler üretilince tekrar etrafları kontrol edilir.
        StartCoroutine(CheckItAll());
    }

    void HexFalls()
    {
        int fallCount = 0;
        bool move = false;
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                while (y < yGrid && hexagons[x, y] == null)
                {
                    fallCount++;
                    y++;
                    move = true;
                }
                if (move)
                {
                    int fallMore = 0;
                    for (int fallingY = y; fallingY < yGrid; fallingY++)
                    {
                        if (hexagons[x, fallingY] == null)
                        {
                            fallMore++;
                            continue;
                        }
                            
                        if(x % 2 == 0)
                            hexagons[x, fallingY].transform.position = Vector3.MoveTowards(hexagons[x, fallingY].transform.position, new Vector3(hexagons[x, fallingY].transform.position.x, fallingY - fallCount - fallMore, 0), 0.05f);
                        else
                            hexagons[x, fallingY].transform.position = Vector3.MoveTowards(hexagons[x, fallingY].transform.position, new Vector3(hexagons[x, fallingY].transform.position.x, fallingY - fallCount - fallMore - 0.5f, 0), 0.1f);

                        hexagons[x, fallingY].GetComponent<Hexagon>().y = fallingY - fallCount - fallMore;
                    }
                }
            }
            fallCount = 0;
            move = false;
        }
    }

    private IEnumerator CheckItAll()
    {
        yield return new WaitForSeconds(0.8f);
        bool success = false;
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                if(CheckNearbyHexs(hexagons[x, y], false, Color.clear, 0, 0))
                {
                    success = true;
                }
            }
        }
        if (success)
        {
            HexCrashDestroy();
        }
        else
        {
            if (!CheckMoveLeft())
            {
                Debug.Log("NO MOVES LEFT! GAME OVER");
                gameOver = true;
                GameOverUI.SetActive(true);
                GameOverUI.transform.GetChild(3).gameObject.SetActive(true);
            }
        }
    }

    void CoordinationUpdate()
    {
        if (rotateRight)
        {
            // 0 = 2, 2 = 1, 1 = 0
            int x = hexSelect.GetChild(0).GetComponent<Hexagon>().x;
            int y = hexSelect.GetChild(0).GetComponent<Hexagon>().y;

            hexagons[x, y] = hexSelect.GetChild(1).gameObject; // 0'ın yerine 1 geldi.
            hexagons[hexSelect.GetChild(1).GetComponent<Hexagon>().x, hexSelect.GetChild(1).GetComponent<Hexagon>().y] = hexSelect.GetChild(2).gameObject; //1'in yerine 2 geldi.
            hexagons[hexSelect.GetChild(2).GetComponent<Hexagon>().x, hexSelect.GetChild(2).GetComponent<Hexagon>().y] = hexSelect.GetChild(0).gameObject; //2'nin yerine 0 geldi.

            hexSelect.GetChild(0).GetComponent<Hexagon>().x = hexSelect.GetChild(2).GetComponent<Hexagon>().x;
            hexSelect.GetChild(0).GetComponent<Hexagon>().y = hexSelect.GetChild(2).GetComponent<Hexagon>().y;

            hexSelect.GetChild(2).GetComponent<Hexagon>().x = hexSelect.GetChild(1).GetComponent<Hexagon>().x;
            hexSelect.GetChild(2).GetComponent<Hexagon>().y = hexSelect.GetChild(1).GetComponent<Hexagon>().y;

            hexSelect.GetChild(1).GetComponent<Hexagon>().x = x;
            hexSelect.GetChild(1).GetComponent<Hexagon>().y = y;
        }
        else
        {
            // 0 = 1, 1 = 2, 2 = 0
            int x = hexSelect.GetChild(0).GetComponent<Hexagon>().x;
            int y = hexSelect.GetChild(0).GetComponent<Hexagon>().y;

            hexagons[x, y] = hexSelect.GetChild(2).gameObject; // 0'ın yerine 1 geldi.
            hexagons[hexSelect.GetChild(1).GetComponent<Hexagon>().x, hexSelect.GetChild(1).GetComponent<Hexagon>().y] = hexSelect.GetChild(0).gameObject; //1'in yerine 2 geldi.
            hexagons[hexSelect.GetChild(2).GetComponent<Hexagon>().x, hexSelect.GetChild(2).GetComponent<Hexagon>().y] = hexSelect.GetChild(1).gameObject;

            hexSelect.GetChild(0).GetComponent<Hexagon>().x = hexSelect.GetChild(1).GetComponent<Hexagon>().x;
            hexSelect.GetChild(0).GetComponent<Hexagon>().y = hexSelect.GetChild(1).GetComponent<Hexagon>().y;

            hexSelect.GetChild(1).GetComponent<Hexagon>().x = hexSelect.GetChild(2).GetComponent<Hexagon>().x;
            hexSelect.GetChild(1).GetComponent<Hexagon>().y = hexSelect.GetChild(2).GetComponent<Hexagon>().y;

            hexSelect.GetChild(2).GetComponent<Hexagon>().x = x;
            hexSelect.GetChild(2).GetComponent<Hexagon>().y = y;
        }
    }

    bool CheckMoveLeft()
    {
        for (int x = 0; x < xGrid; x++)
        {
            for (int y = 0; y < yGrid; y++)
            {
                if(x%2 == 0)
                {
                    if (x != 0 && y != yGrid - 1)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x - 1, y + 1].GetComponent<Renderer>().material.color, -1, 1))
                            return true;
                    if (x != 0)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x - 1, y].GetComponent<Renderer>().material.color, -1, 0))
                            return true;
                    if (y != yGrid - 1)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x, y + 1].GetComponent<Renderer>().material.color, 0, 1))
                            return true;
                    if (y != 0)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x, y - 1].GetComponent<Renderer>().material.color, 0, -1))
                            return true;
                    if (x != xGrid - 1 && y != yGrid - 1)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x + 1, y + 1].GetComponent<Renderer>().material.color, 1, 1))
                            return true;
                    if (x != xGrid - 1)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x + 1, y].GetComponent<Renderer>().material.color, 1, 0))
                            return true;
                }
                else
                {
                    if (x != 0)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x - 1, y].GetComponent<Renderer>().material.color, -1, 0))
                            return true;
                    if (x != 0 && y != 0)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x - 1, y - 1].GetComponent<Renderer>().material.color, -1, -1))
                            return true;
                    if (y != yGrid - 1)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x, y + 1].GetComponent<Renderer>().material.color, 0, 1))
                            return true;
                    if (y != 0)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x, y - 1].GetComponent<Renderer>().material.color, 0, -1))
                            return true;
                    if (x != xGrid - 1)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x + 1, y].GetComponent<Renderer>().material.color, 1, 0))
                            return true;
                    if (x != xGrid - 1 && y != 0)
                        if (CheckNearbyHexs(hexagons[x, y], true, hexagons[x + 1, y - 1].GetComponent<Renderer>().material.color, 1, -1))
                            return true;
                }
            }
        }
        return false;
    }

    bool CheckNearbyHexs(GameObject hexToCheck, bool gameOverCheck, Color foreSee, int x, int y)
    {
        bool success = false;

        int hexX = hexToCheck.GetComponent<Hexagon>().x;
        int hexY = hexToCheck.GetComponent<Hexagon>().y;

        Color color1;
        if (gameOverCheck)
            color1 = foreSee;
        else
            color1 = hexToCheck.GetComponent<Renderer>().material.color;

        Color color2;
        Color color3 = Color.black;

        if (hexX % 2 == 0)
        {
            if(hexX != xGrid - 1 && !(gameOverCheck && x == 1 && y == 0))
            {
                color2 = hexagons[hexX + 1, hexY].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != xGrid - 1 && hexY != yGrid - 1)
                        color2 = hexagons[hexX + 1, hexY + 1].GetComponent<Renderer>().material.color;
                    if(hexY != 0)
                        color3 = hexagons[hexX, hexY - 1].GetComponent<Renderer>().material.color;

                    if (hexX != xGrid - 1 && hexY != yGrid - 1 && !(gameOverCheck && x == 1 && y == 1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sag alt " + hexagons[hexX + 1, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY + 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexY != 0 && !(gameOverCheck && x == 0 && y == -1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sag alt " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }
            
            if(hexX != xGrid - 1 && hexY != yGrid - 1 && !(gameOverCheck && x == 1 && y == 1))
            {
                color2 = hexagons[hexX + 1, hexY + 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != xGrid - 1)
                        color2 = hexagons[hexX + 1, hexY].GetComponent<Renderer>().material.color;
                    if(hexY != yGrid - 1)
                        color3 = hexagons[hexX, hexY + 1].GetComponent<Renderer>().material.color;

                    if (hexX != xGrid - 1 && !(gameOverCheck && x == 1 && y == 0) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sag ust " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexY != yGrid - 1 && !(gameOverCheck && x == 0 && y == 1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sag ust " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexY != yGrid - 1 && !(gameOverCheck && x == 0 && y == 1))
            {
                color2 = hexagons[hexX, hexY + 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != xGrid - 1 && hexY != yGrid - 1)
                        color2 = hexagons[hexX + 1, hexY + 1].GetComponent<Renderer>().material.color;
                    if(hexX != 0 && hexY != yGrid - 1)
                        color3 = hexagons[hexX - 1, hexY + 1].GetComponent<Renderer>().material.color;

                    if (hexX != xGrid - 1 && hexY != yGrid - 1 && !(gameOverCheck && x == 1 && y == 1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " ust " + hexagons[hexX + 1, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY + 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexX != 0 && hexY != yGrid - 1 && !(gameOverCheck && x == -1 && y == 1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " ust " + hexagons[hexX - 1, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY + 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }
            
            if(hexX != 0 && hexY != yGrid - 1 && !(gameOverCheck && x == -1 && y == 1))
            {
                color2 = hexagons[hexX - 1, hexY + 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexY != yGrid - 1)
                        color2 = hexagons[hexX, hexY + 1].GetComponent<Renderer>().material.color;
                    if(hexX != 0)
                        color3 = hexagons[hexX - 1, hexY].GetComponent<Renderer>().material.color;

                    if (hexY != yGrid - 1 && !(gameOverCheck && x == 0 && y == 1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sol ust " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexX != 0 && !(gameOverCheck && x == -1 && y == 0) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sol ust " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexX != 0 && !(gameOverCheck && x == -1 && y == 0))
            {
                color2 = hexagons[hexX - 1, hexY].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != 0 && hexY != yGrid - 1)
                        color2 = hexagons[hexX - 1, hexY + 1].GetComponent<Renderer>().material.color;
                    if(hexY != 0)
                        color3 = hexagons[hexX, hexY - 1].GetComponent<Renderer>().material.color;

                    if (hexX != 0 && hexY != yGrid - 1 && !(gameOverCheck && x == -1 && y == 1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sol alt " + hexagons[hexX - 1, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY + 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexY != 0 && !(gameOverCheck && x == 0 && y == -1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sol alt " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexY != 0 && !(gameOverCheck && x == 0 && y == -1))
            {
                color2 = hexagons[hexX, hexY - 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != 0)
                        color2 = hexagons[hexX - 1, hexY].GetComponent<Renderer>().material.color;
                    if(hexX != xGrid - 1)
                        color3 = hexagons[hexX + 1, hexY].GetComponent<Renderer>().material.color;

                    if (hexX != 0 && !(gameOverCheck && x == -1 && y == 0) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " alt " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexX != xGrid - 1 && !(gameOverCheck && x == 1 && y == 0) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " alt " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }
        }
        else
        {
            if(hexX != xGrid - 1 && hexY != 0 && !(gameOverCheck && x == 1 && y == -1))
            {
                color2 = hexagons[hexX + 1, hexY - 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != xGrid - 1)
                        color2 = hexagons[hexX + 1, hexY].GetComponent<Renderer>().material.color;
                    if(hexY != 0)
                        color3 = hexagons[hexX, hexY - 1].GetComponent<Renderer>().material.color;

                    if (hexX != xGrid - 1 && !(gameOverCheck && x == 1 && y == 0) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sag alt " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexY != 0 && !(gameOverCheck && x == 0 && y == -1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sag alt" + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexX != xGrid - 1 && !(gameOverCheck && x == 1 && y == 0))
            {
                color2 = hexagons[hexX + 1, hexY].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != xGrid - 1 && hexY != 0)
                        color2 = hexagons[hexX + 1, hexY - 1].GetComponent<Renderer>().material.color;
                    if(hexY != yGrid - 1)
                        color3 = hexagons[hexX, hexY + 1].GetComponent<Renderer>().material.color;

                    if (hexX != xGrid - 1 && hexY != 0 && !(gameOverCheck && x == 1 && y == -1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sag ust " + hexagons[hexX + 1, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY - 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexY != yGrid - 1 && !(gameOverCheck && x == 0 && y == 1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sag ust " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX + 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexY != yGrid - 1 && !(gameOverCheck && x == 0 && y == 1))
            {
                color2 = hexagons[hexX, hexY + 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != xGrid - 1)
                        color2 = hexagons[hexX + 1, hexY].GetComponent<Renderer>().material.color;
                    if(hexX != 0)
                        color3 = hexagons[hexX - 1, hexY].GetComponent<Renderer>().material.color;

                    if (hexX != xGrid - 1 && !(gameOverCheck && x == 1 && y == 0) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " ust " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexX != 0 && !(gameOverCheck && x == -1 && y == 0) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " ust " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY + 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexX != 0 && !(gameOverCheck && x == -1 && y == 0))
            {
                color2 = hexagons[hexX - 1, hexY].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexY != yGrid - 1)
                        color2 = hexagons[hexX, hexY + 1].GetComponent<Renderer>().material.color;
                    if(hexX != 0 && hexY != 0)
                        color3 = hexagons[hexX - 1, hexY - 1].GetComponent<Renderer>().material.color;

                    if (hexY != yGrid - 1 && !(gameOverCheck && x == 0 && y == 1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sol ust " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY + 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexX != 0 && hexY != 0 && !(gameOverCheck && x == -1 && y == -1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sol ust" + hexagons[hexX - 1, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY - 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexX != 0 && hexY != 0 && !(gameOverCheck && x == -1 && y == -1))
            {
                color2 = hexagons[hexX - 1, hexY - 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != 0)
                        color2 = hexagons[hexX - 1, hexY].GetComponent<Renderer>().material.color;
                    if(hexY != 0)
                        color3 = hexagons[hexX, hexY - 1].GetComponent<Renderer>().material.color;

                    if (hexX != 0 && !(gameOverCheck && x == -1 && y == 0) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " sol alt " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexY != 0 && !(gameOverCheck && x == 0 && y == -1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " sol alt " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX, hexY - 1].GetComponent<Hexagon>().y + " " + color3);
                        if (!gameOverCheck)
                            hexagons[hexX - 1, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }

            if(hexY != 0 && !(gameOverCheck && x == 0 && y == -1))
            {
                color2 = hexagons[hexX, hexY - 1].GetComponent<Renderer>().material.color;
                if (color1.Equals(color2))
                {
                    if(hexX != 0 && hexY != 0)
                        color2 = hexagons[hexX - 1, hexY - 1].GetComponent<Renderer>().material.color;
                    if(hexX != xGrid - 1 && hexY != 0)
                        color3 = hexagons[hexX + 1, hexY - 1].GetComponent<Renderer>().material.color;

                    if (hexX != 0 && hexY != 0 && !(gameOverCheck && x == -1 && y == -1) && color1.Equals(color2))
                    {
                        Debug.Log(hexX + "," + hexY + " alt " + hexagons[hexX - 1, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX - 1, hexY - 1].GetComponent<Hexagon>().y + " " + color2);
                        if (!gameOverCheck)
                            hexagons[hexX, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                    else if (hexX != xGrid - 1 && hexY != 0 && !(gameOverCheck && x == 1 && y == -1) && color1.Equals(color3))
                    {
                        Debug.Log(hexX + "," + hexY + " alt " + hexagons[hexX + 1, hexY - 1].GetComponent<Hexagon>().x + " " + hexagons[hexX + 1, hexY - 1].GetComponent<Hexagon>().y + " " + color3);
                        if(!gameOverCheck)
                            hexagons[hexX, hexY - 1].GetComponent<Hexagon>().destroy = true;
                        success = true;
                    }
                }
            }
        }
        if (success)
        {
            if(!gameOverCheck)
                hexagons[hexX, hexY].GetComponent<Hexagon>().destroy = true;
            Debug.Log(hexX + "," + hexY + " alt " + hexagons[hexX, hexY].GetComponent<Renderer>().material.color);
        }

        return success;
    }

    void SelectHex()
    {
        touch = Input.GetTouch(0);
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(touch.position);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                beganPos = touch.position;
                break;
            case TouchPhase.Moved:
                direction = touch.position - beganPos;
                if (hexSelect.childCount == 3 && direction.magnitude > 100 && !rotate)
                {
                    Debug.Log("rotate");
                    //Sağa veya sola döndürme;
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                    {
                        if (direction.x > 0 && worldPoint.y > hexSelect.position.y)
                        {
                            rotateRight = true;
                        }
                        else if (direction.x < 0 && worldPoint.y > hexSelect.position.y)
                        {
                            rotateRight = false;
                        }
                        else if (direction.x > 0 && worldPoint.y < hexSelect.position.y)
                        {
                            rotateRight = false;
                        }
                        else if (direction.x < 0 && worldPoint.y < hexSelect.position.y)
                        {
                            rotateRight = true;
                        }
                    }
                    //Yukarı yada aşağı döndürme;
                    else
                    {
                        if (worldPoint.x > hexSelect.position.x && direction.y > 0)
                        {
                            rotateRight = false;
                        }
                        else if (worldPoint.x > hexSelect.position.x && direction.y < 0)
                        {
                            rotateRight = true;
                        }
                        else if (worldPoint.x < hexSelect.position.x && direction.y > 0)
                        {
                            rotateRight = true;
                        }
                        else if (worldPoint.x < hexSelect.position.x && direction.y < 0)
                        {
                            rotateRight = false;
                        }
                    }
                    hexSelectRotation = hexSelect.rotation.eulerAngles.z;
                    rotateStart = true;
                    rotate = true;
                    StartCoroutine(RotateFinish());
                }

                break;
            case TouchPhase.Ended:
                if (rotate)
                {
                    rotate = false;
                    break;
                }

                LeaveHexSelect();
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                {
                    int hexGridX = hitInfo.collider.GetComponent<Hexagon>().x;
                    int hexGridY = hitInfo.collider.GetComponent<Hexagon>().y;

                    //Altıgenin sağ yüzeyine mi basıldı?
                    if (worldPoint.x > hitInfo.collider.transform.position.x && hexGridX != xGrid - 1 || hexGridX == 0)
                    {
                        //Üst köşe mi?
                        if ((worldPoint.y > hitInfo.collider.transform.position.y + 0.2165f || hexGridY == 0 && hexGridX % 2 == 1) && hexGridY != yGrid - 1)
                        {
                            hexSelect.rotation = Quaternion.Euler(0, 0, 150);
                            hexSelect.position = hitInfo.collider.transform.position + new Vector3(0.295f, 0.5f, -0.1f);
                            //0,0 - 0,1 - 1,1
                            //1,0 - 1,1 - 2,0
                            hexagons[hexGridX, hexGridY].transform.parent = transform.GetChild(0);
                            if (hexGridX % 2 == 0)
                            {
                                hexagons[hexGridX + 1, hexGridY + 1].transform.parent = transform.GetChild(0);
                            }
                            else
                            {
                                hexagons[hexGridX + 1, hexGridY].transform.parent = transform.GetChild(0);
                            }
                            hexagons[hexGridX, hexGridY + 1].transform.parent = transform.GetChild(0);
                        }
                        //Alt köşe mi?
                        else if ((worldPoint.y < hitInfo.collider.transform.position.y - 0.2165f && !(hexGridY == 0 && hexGridX % 2 == 0)) || (hexGridY == yGrid - 1 && hexGridX % 2 == 0))
                        {
                            hexSelect.rotation = Quaternion.Euler(0, 0, 150);
                            hexSelect.position = hitInfo.collider.transform.position + new Vector3(0.295f, -0.5f, -0.1f);
                            //0,1 - 0,0 - 1,1
                            //1,1 - 1,0 - 2,0
                            hexagons[hexGridX, hexGridY].transform.parent = transform.GetChild(0);
                            hexagons[hexGridX, hexGridY - 1].transform.parent = transform.GetChild(0);
                            if (hexGridX % 2 == 0)
                            {
                                hexagons[hexGridX + 1, hexGridY].transform.parent = transform.GetChild(0);
                            }
                            else
                            {
                                hexagons[hexGridX + 1, hexGridY - 1].transform.parent = transform.GetChild(0);
                            }
                        }
                        //Orta köşe mi?
                        else
                        {
                            hexSelect.rotation = Quaternion.Euler(0, 0, 90);
                            hexSelect.position = hitInfo.collider.transform.position + new Vector3(0.57f, 0, -0.1f);
                            //0,0 - 1,0 - 1,1
                            //1,1 - 2,1 - 2,0
                            hexagons[hexGridX, hexGridY].transform.parent = transform.GetChild(0);
                            
                            if (hexGridX % 2 == 0)
                            {
                                hexagons[hexGridX + 1, hexGridY].transform.parent = transform.GetChild(0);
                                hexagons[hexGridX + 1, hexGridY + 1].transform.parent = transform.GetChild(0);
                            }
                            else
                            {
                                hexagons[hexGridX + 1, hexGridY - 1].transform.parent = transform.GetChild(0);
                                hexagons[hexGridX + 1, hexGridY].transform.parent = transform.GetChild(0);
                            }
                        }
                    }
                    //Altıgenin sol yüzeyine mi basıldı?
                    else
                    {
                        //Üst köşe mi?
                        if ((worldPoint.y > hitInfo.collider.transform.position.y + 0.2165f || hexGridY == 0 && hexGridX % 2 == 1) && hexGridY != yGrid - 1)
                        {
                            hexSelect.rotation = Quaternion.Euler(0, 0, 90);
                            hexSelect.position = hitInfo.collider.transform.position + new Vector3(-0.295f, 0.5f, -0.1f);
                            //1,0 - 1,1 - 0,0
                            //2,0 - 2,1 - 1,1
                            hexagons[hexGridX, hexGridY].transform.parent = transform.GetChild(0);
                            hexagons[hexGridX, hexGridY + 1].transform.parent = transform.GetChild(0);
                            if (hexGridX % 2 == 0)
                            {
                                hexagons[hexGridX - 1, hexGridY + 1].transform.parent = transform.GetChild(0);
                            }
                            else
                            {
                                hexagons[hexGridX - 1, hexGridY].transform.parent = transform.GetChild(0);
                            }
                        }
                        //Alt köşe mi?
                        else if ((worldPoint.y < hitInfo.collider.transform.position.y - 0.2165f && !(hexGridY == 0 && hexGridX % 2 == 0)) || (hexGridY == yGrid - 1 && hexGridX % 2 == 0))
                        {
                            hexSelect.rotation = Quaternion.Euler(0, 0, 90);
                            hexSelect.position = hitInfo.collider.transform.position + new Vector3(-0.295f, -0.5f, -0.1f);
                            //2,1 - 2,0 - 1,1
                            //1,1 - 1,0 - 0,0
                            hexagons[hexGridX, hexGridY].transform.parent = transform.GetChild(0);
                            if (hexGridX % 2 == 0)
                            {
                                hexagons[hexGridX - 1, hexGridY].transform.parent = transform.GetChild(0);
                            }
                            else
                            {
                                hexagons[hexGridX - 1, hexGridY - 1].transform.parent = transform.GetChild(0);
                            }
                            hexagons[hexGridX, hexGridY - 1].transform.parent = transform.GetChild(0);
                        }
                        //Orta köşe mi?
                        else
                        {
                            hexSelect.rotation = Quaternion.Euler(0, 0, 150);
                            hexSelect.position = hitInfo.collider.transform.position + new Vector3(-0.57f, 0, -0.1f);
                            //2,0 - 1,0 - 1,1
                            //1,1 - 0,1 - 0,0
                            hexagons[hexGridX, hexGridY].transform.parent = transform.GetChild(0);
                            
                            if (hexGridX % 2 == 0)
                            {
                                hexagons[hexGridX - 1, hexGridY + 1].transform.parent = transform.GetChild(0);
                                hexagons[hexGridX - 1, hexGridY].transform.parent = transform.GetChild(0);
                            }
                            else
                            {
                                hexagons[hexGridX - 1, hexGridY].transform.parent = transform.GetChild(0);
                                hexagons[hexGridX - 1, hexGridY - 1].transform.parent = transform.GetChild(0);
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
    }
}
