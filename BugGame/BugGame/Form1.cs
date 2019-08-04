using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BugGame
{
    public partial class Form1 : Form
    {
        const int BAB_NUM = 30;     //벌레가 잡아먹을 BAB을 생성할 갯수입니다. 생성되는 game_pan배열의 요소 값은 2입니다.
        /* 
           게임 판 배열을 생성합니다.
           2차원 배열로 생성된 게임 판의 각 좌표에 서로 다른 정수값을 할당함으로써, 게임 제작자가 게임 판의 각 위치에 어떤 게임 요소가 존재하는지 쉽게 파악하도록 합니다.
           아래는 사용할 정수와 그에 해당하는 게임 요소입니다.
           -1 = 벽
           0 = 빈 공간
           3 = 플레이어 1의 머리 및 꼬리
           4 = 플레이어 2의 머리 및 꼬리
        */
        int[,] game_pan = new int[33, 33];      

        int len1, len2;     //플레이어 1, 플레이어 2의 벌레의 전체 길이입니다.
        //플레이어 1의 머리와 꼬리들의 위치를 배열로 저장합니다.
        int[] bug1X = new int[40];          
        int[] bug1Y = new int[40];
        //플레이어 2의 머리와 꼬리들의 위치를 배열로 저장합니다.
        int[] bug2X = new int[40];
        int[] bug2Y = new int[40];

        int xDir1, yDir1, xDir2, yDir2;     //플레이어 1과 플레이어 2의 x, y축 이동 방향을 저장합니다.

        int eatCtr = 0;     //먹은 BAB의 갯수를 저장합니다. 30이 되면 게임이 종료됩니다.

        /*
           각 플레이어의 이동 상태는 게임 판의 요소들에 의해 변경될 수 있습니다.
           예를 들어, 벽을 만나거나 자기 자신의 꼬리와 만났을 경우 정지해야 하며
           빈 공간에서나 상대 플레이어의 꼬리나 머리와 만났을 경우 이동 가능해야 합니다.
           이를 MovingBug메소드에서 파악하여, 전역변수 moveJudge에 각각 다른 정수값을 할당합니다.
           time에서 각각 달라지는 moveJudge값을 이용해 플레이어들을 이동시킵니다.
        */
        int moveJudge1;
        int moveJudge2;
        
        //moveJudge에 할당될 상수들입니다. 구분이 가능하다면 숫자로만 할당하여도 무방하나, 가독성을 높이기 위해 지정하였습니다.
        const int STOP = -1;
        const int MOVE = 0;
        const int EAT_BAB = 2;
        const int P1MEET = 3;
        const int P2MEET = 4; 

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
            //타이머를 Form1.cs에서 제어합니다. 디자인 패널에서 수정 시 유의해주세요.
            timer1.Start();
            timer1.Interval = 100;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GamePanInit();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawGamePan(e.Graphics);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            CursorControl(e.KeyCode);
        }



        void babGenerator()
        {
            int i, x, y;
            Random rand = new Random();

            for (i = 0; i < BAB_NUM; i++)
            {
                x = rand.Next() % 31 + 1;
                y = rand.Next() % 31 + 1;
                if (game_pan[y, x] == 0)
                    game_pan[y, x] = 2;
                else
                {
                    i = i - 1;
                    continue;
                }
            }
            return;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            MovingBug();            //moveJudge1, moveJudge2의 갱신을 위해 호출합니다
            /*
               갱신된 moveJudge1, moveJudge2에 따라 서로 다른 동작을 수행합니다. 각 상수별 할당된 정수는 다음과 같습니다.
               STOP = -1
               MOVE = 0
               EAT_BAB = 2
               P1MEET = 3
               P2MEET = 4
               이는 game_pan의 각 좌표에 할당된 요소와 같습니다. 단순히 가독성을 높이기 위한 장치로서, 숫자를 직접 기입해도 무방합니다.
            */

            switch (moveJudge1)
            {
                case STOP:      //다음 공간으로 이동이 불가능한 경우입니다. 아무런 이동 동작도 취하지 않고 switch 문을 빠져나옵니다.
                    break;

                /*
                  다음 공간이 비어있는 경우와 player2의 꼬리인 경우입니다. 아래는 player1의 이동 동작입니다.
                  이동 동작시 머리를 xDir, yDir으로 이동시키고, 이후 각 꼬리들을 한칸씩 이어붙입니다.
                  동시에 game_pan의 빈공간의 요소가 0, 꼬리와 머리의 요소가 3인 점을 고려하여 game_pan의 요소들 역시 이에 맞게 업데이트합니다.
                  해당 소스에서는 이동하는 경우에는 머리와 꼬리사이 좌표의 game_pan의 요소는 그대로 두고, 머리가 새롭게 이동하는 좌표에 3을, 꼬리가 없어지는 좌표에 0을 할당합니다.
                  (단, '서로 통과하는 경우' 는 엄밀히 말해, 논리적으로 한가지 경우만 존재하지 않음에 유의해야 합니다.
                  '서로 통과하는 경우'를 프로그래밍적으로 표현한다면, Plyaer1이 Plyaer2를 만나도 빈공간을 통과할 때와 마찬가지로, 각 머리와 꼬리가 이동하여
                  서로가 서로의 요소를 갱신하는 경우를 뜻합니다. 
                  그것 뿐이라 생각하기 쉽지만, 서로 통과하는 경우는 양 측이 모두 움직이는 경우뿐만이 아닌, 한 쪽은 정지해있고, 한 쪽은 이동하는 경우도 있습니다.
                  이 경우, 정지해 있는 쪽은 갱신이 일어나서는 안되며, 이동중인 쪽에서만 갱신이 일어나야 합니다.
                  즉 '서로 통과하는 경우'에 대해서는, 두가지 서로 다른 조건에 대한 처리를 다르게 해 줘야 합니다.)
                */

                case MOVE:      //P1의 다음 이동 위치가 빈 공간인 경우
                case P2MEET:    //P1의 다음 이동 위치가 P2의 머리나 꼬리인 경우
                    if (xDir1 == 0 && yDir1 == 0)   //P1은 정지중이고, P2만 움직이는 경우, P1은 갱신되어서는 안됩니다.
                    {
                        break;
                    }
                    else
                    {
                        UpdateBug1();               //P1이 이동중인 경우, P1은 갱신되어야합니다.
                        break;
                    }

                case EAT_BAB:   //P1이 BAB을 만난 경우
                    game_pan[bug1Y[0] + yDir1, bug1X[0] + xDir1] = 0;   //BAB의 좌표의 game_pan 요소를 0(빈공간)으로 바꿔주고
                    bug1X[len1] = bug1X[len1 - 1];                      //새로운 꼬리를 생성하여 이어붙입니다.
                    bug1Y[len1] = bug1Y[len1 - 1];
                    game_pan[bug1Y[len1 - 1], bug1X[len1 - 1]] = 3;     //새로이 생성된 자리의 game_pan 요소도 변경해줍니다.
                    len1 += 1;                                          //추가된 꼬리의 길이를 반영해줍니다.
                    eatCtr += 1;                                        //BAB을 먹은 갯수를 세어줍니다. ending 조건과 연결됩니다.

                    UpdateBug1();                                       //이 위까지 꼬리를 추가하는 소스입니다다. 추가가 끝났으면, 이동시켜줍니다. 순서가 바뀌어도 상관없습니다.

                    break;
            }

            //moveJudge2의 경우 moveJudge1의 경우와 대칭됩니다. 논리는 서로 같습니다.
            switch (moveJudge2)
            {
                case STOP:
                    break;

                case MOVE:
                case P1MEET:
                    if (xDir2 == 0 && yDir2 == 0)
                    {
                        break;
                    }
                    else
                    {
                        UpdateBug2();
                        break;
                    }

                case EAT_BAB:
                    game_pan[bug2Y[0] + yDir2, bug2X[0] + xDir2] = 0;
                    bug2X[len2] = bug2X[len2 - 1];
                    bug2Y[len2] = bug2Y[len2 - 1];
                    game_pan[bug2Y[len2 - 1], bug2X[len2 - 1]] = 4;
                    len2 += 1;
                    eatCtr += 1;

                    UpdateBug2();

                    break;
            }

            //생성된 밥의 갯수만큼 밥을 먹었다면, 게임을 종료합니다.
            if (eatCtr == BAB_NUM)
            {
                timer1.Stop();
                if (len1 > len2) MessageBox.Show("Player1 win!");
                else if (len1 == len2) MessageBox.Show("Draw!");
                else MessageBox.Show("Player2 win!");
            }
            Refresh();
        }

        void GamePanInit()
        {
            int i;
            for (i = 0; i < 33; i++)
            {
                game_pan[i, 0] = -1;
                game_pan[i, 32] = -1;
                game_pan[0, i] = -1;
                game_pan[32, i] = -1;
            }
            
            bug1X[0] = 2; bug1Y[0] = 1;         //플레이어1의 초기 머리 위치입니다.
            bug1X[1] = 1; bug1Y[1] = 1;         //플레이어1의 초기 두번째 꼬리 위치입니다.

            bug2X[0] = 30; bug2Y[0] = 1;        //플레이어2의 초기 머리 위치입니다.
            bug2X[1] = 31; bug2Y[1] = 1;        //플레이어2의 초기 두번째 꼬리 위치입니다.

            /*
               각 플레이어의 좌표에 맞게끔 게임 판에 요소를 설정해줍니다.
               플레이어 1의 구성요소들엔 3을, 플레이어 2의 구성요소들엔 4를 설정합니다.
            */
            game_pan[bug1Y[0], bug1X[0]] = 3;   
            game_pan[bug1Y[1], bug1X[1]] = 3;

            game_pan[bug2Y[0], bug2X[0]] = 4;   
            game_pan[bug2Y[1], bug2X[1]] = 4;

            babGenerator();     //밥을 랜덤한 위치에 생성합니다.

            //플레이어 1, 2의 초기 전체 길이 및 초기 방향을 설정합니다.
            len1 = 2;
            xDir1 = 0; yDir1 = 0;

            len2 = 2;
            xDir2 = 0; yDir2 = 0;
        }


        void DrawGamePan(Graphics g)
        {
            int x, y, i;

            Pen blackPen = new Pen(Color.Black);
            Pen redPen = new Pen(Color.Red);
            Pen bluePen = new Pen(Color.Blue);

            //게임 판의 각 요소를 확인하여, 알맞은 도형을 그려줍니다.
            for (y = 0; y < 33; y++)
            {
                for (x = 0; x < 33; x++)
                {
                    switch (game_pan[y, x])
                    {
                        //게임 판의 요소가 -1인 경우, 벽에 해당하는 검정 사각형을 그려줍니다.
                        case -1:
                            g.DrawRectangle(blackPen, x * 20, y * 20, 20, 20);
                            break;
                        //게임 판의 요소가 2인 경우, BAB에 해당하는 속이 꽉 찬 검정 원을 그려줍니다.
                        case 2:
                            SolidBrush blackBrush = new SolidBrush(Color.Black);
                            g.FillEllipse(blackBrush, x * 20, y * 20, 20, 20);
                            break;
                    }
                }
            }

            //각 플레이어의 머리 위치에 빨강색 원을, 꼬리 위치에 파랑색 원을 그려줍니다.
            g.DrawEllipse(redPen, bug1X[0] * 20, bug1Y[0] * 20, 20, 20);
            for (i = 1; i < len1; i++)
                g.DrawEllipse(bluePen, bug1X[i] * 20, bug1Y[i] * 20, 20, 20);
            
            g.DrawEllipse(redPen, bug2X[0] * 20, bug2Y[0] * 20, 20, 20);
            for (i = 1; i < len2; i++)
                g.DrawEllipse(bluePen, bug2X[i] * 20, bug2Y[i] * 20, 20, 20);
                
        }

        void CursorControl(Keys DirectKey)
        {
            //입력되는 사용자 키를 통해, 플레이어 1과 2의 다음 이동 방향을 설정합니다.
            switch (DirectKey)
            {
                case Keys.Left:
                    if (xDir1 == 1)
                        break;
                    if (game_pan[bug1Y[0], bug1X[0] - 1] != -1)
                    {
                        xDir1 = -1;
                        yDir1 = 0;
                    }
                    break;
                case Keys.Right:
                    if (xDir1 == -1)
                        break;
                    if (game_pan[bug1Y[0], bug1X[0] + 1] != -1)
                    {
                        xDir1 = 1;
                        yDir1 = 0;
                    }
                    break;
                case Keys.Up:
                    if (yDir1 == 1)
                        break;
                    if (game_pan[bug1Y[0] - 1, bug1X[0]] != -1)
                    {
                        xDir1 = 0;
                        yDir1 = -1;
                    }
                    break;
                case Keys.Down:
                    if (yDir1 == -1)
                        break;
                    if (game_pan[bug1Y[0] + 1, bug1X[0]] != -1)
                    {
                        xDir1 = 0;
                        yDir1 = 1;
                    }
                    break;

                case Keys.A:
                    if (xDir2 == 1)
                        break;
                    if (game_pan[bug2Y[0], bug2X[0] - 1] != -1)
                    {
                        xDir2 = -1;
                        yDir2 = 0;
                    }
                    break;
                case Keys.D:
                    if (xDir2 == -1)
                        break;
                    if (game_pan[bug2Y[0], bug2X[0] + 1] != -1)
                    {
                        xDir2 = 1;
                        yDir2 = 0;
                    }
                    break;
                case Keys.W:
                    if (yDir2 == 1)
                        break;
                    if (game_pan[bug2Y[0] - 1, bug2X[0]] != -1)
                    {
                        xDir2 = 0;
                        yDir2 = -1;
                    }
                    break;
                case Keys.S:
                    if (yDir2 == -1)
                        break;
                    if (game_pan[bug2Y[0] + 1, bug2X[0]] != -1)
                    {
                        xDir2 = 0;
                        yDir2 = 1;
                    }
                    break;

            }
            //MovingBug();
            Invalidate();
        }
        



        void MovingBug()
        {
            /*
              각각의 moveFlag는 각 벌레의 머리가 다음 순간에 이동하고자 하는 위치에 어떤 game_pan배열의 값이 저장되어있는지 지정합니다.
              -1 = 벽
              2 = BAB
              3 = Player1의 구성요소(머리 및 꼬리)
              4 = Player2의 구성요소(머리 및 꼬리)
            */
            int moveFlag1 = game_pan[bug1Y[0] + yDir1, bug1X[0] + xDir1];
            int moveFlag2 = game_pan[bug2Y[0] + yDir2, bug2X[0] + xDir2];


            //각각의 switch문을 통해, 전역변수 moveJudge를 통제합니다. moveJudge를 통해 Timer에서 머리와 꼬리의 다음 동작이 결정됩니다.

            switch (moveFlag1)
            {
                //머리가 BAB을 만날 경우
                case 2:
                    moveJudge1 = EAT_BAB;
                    break;
                //머리가 3(자기자신의 꼬리) 혹은 벽을 만날 경우
                case 3:
                case -1:
                    moveJudge1 = STOP;
                    break;
                //머리가 이동가능한 경우. 해당 소스에선 case 0:으로 설정하여도 무방합니다.
                default:
                    moveJudge1 = MOVE;
                    break;
            }

            switch (moveFlag2)
            {
                //머리가 BAB을 만날 경우
                case 2:
                    moveJudge2 = EAT_BAB;
                    break;
                //머리가 4(자기자신의 꼬리) 혹은 벽을 만날 경우
                case 4:
                case -1:
                    moveJudge2 = STOP;
                    break;
                //머리가 이동가능한 경우. 해당 소스에선 case 0:으로 설정하여도 무방합니다.
                default:
                    moveJudge2 = MOVE;
                    break;
            }

        }


        void UpdateBug1()
        {
            game_pan[bug1Y[len1 - 1], bug1X[len1 - 1]] = 0;
            for (int i = len1 - 1; i > 0; i--)
            {
                bug1X[i] = bug1X[i - 1];
                bug1Y[i] = bug1Y[i - 1];
            }
            bug1X[0] += xDir1;
            bug1Y[0] += yDir1;
            game_pan[bug1Y[0], bug1X[0]] = 3;
        }

        void UpdateBug2()
        {
            game_pan[bug2Y[len2 - 1], bug2X[len2 - 1]] = 0;
            for (int i = len2 - 1; i > 0; i--)
            {
                bug2X[i] = bug2X[i - 1];
                bug2Y[i] = bug2Y[i - 1];
            }
            bug2X[0] += xDir2;
            bug2Y[0] += yDir2;
            game_pan[bug2Y[0], bug2X[0]] = 4;
        }
    }
}
