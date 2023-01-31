using ProcessMemoryReaderLib;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace AssaltCubeGameHack
{
    public partial class Form1 : Form
    {
        Process[] MyProcess;    // 프로세스를 목록을 저장할 장소
        ProcessMemoryReader mem = new ProcessMemoryReader();
        Process attachProc;
        OverlayForm overlayForm = new OverlayForm();

        bool attach = false;
        bool healthHack = false;
        bool ammoHack = false;
        bool wallHack = false;
        bool superjump = false;
        bool attackHack = false;

        PlayerData mainPlayer;
        PlayerData[] enemyPlayer = new PlayerData[30];
        


        public Form1()
        {
            InitializeComponent();
        }

        private void ExitBT_Click(object sender, EventArgs e)
        {
            DialogResult result;    // 정말 종료할 것인지 확인하는 변수
            result = MessageBox.Show("종료하시겠습니까?", "종료메시지", MessageBoxButtons.OKCancel);

            if (result == DialogResult.OK)
            {
                this.DialogResult = DialogResult.Abort;
                Application.Exit();
            }
        }

        // 클릭했을 때 프로세스 목록 나타나도록 하는 함수
        private void comboBox1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();    // 기존 프로세스 목록을 초기화
            MyProcess = Process.GetProcesses(); // 프로세스 목록 불러오기

            for (int i = 0; i < MyProcess.Length; i++)
            {
                string text = MyProcess[i].ProcessName + "-" + MyProcess[i].Id;
                comboBox1.Items.Add(text);
            }

        }


        // 콤보박스 메뉴중에 어떤 항목을 클릭했을 때 동작할 내용
        // 프로세스를 선택했을 때 어떤 행동을 할지 정하기
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // 해당 프로세스에 대한 권한 가져오기
                // 프로세스 메모리 읽기/수정
                if (comboBox1.SelectedIndex != -1)  // 목록을 선택했다면
                {
                    string selectedItem = comboBox1.SelectedItem.ToString();    // ex. KakaoTalk-704
                    int pid = int.Parse(selectedItem.Split('-')[selectedItem.Split('-').Length - 1]); // 문자열을 -로 나눈 후 가장 마지막 문자열으르 가져오기
                    attachProc = Process.GetProcessById(pid);

                    // 프로세스 열기 (권한)
                    mem.ReadProcess = attachProc;
                    mem.OpenProcess(); 

                    MessageBox.Show("프로세스 열기 성공! " + attachProc.ProcessName);
                    int base_ptr = attachProc.MainModule.BaseAddress.ToInt32() + 0x0055C424;
                    int player_base = mem.ReadInt(base_ptr);
                    mainPlayer = new PlayerData(player_base);

                    attach = true;
                }
            }
            catch (Exception ex)    // 예외 처리
            {
                attach = false;
                MessageBox.Show("프로세스 열기 실패: " + ex.Message);
            }
            
        }

        private void timer1_Tick(object sender, EventArgs e)    // 0.001초마다 동작
        {
            if (attach)
            {
                try
                {
                    mainPlayer.SetPlayerData(mem);
                    if (healthHack)
                    {
                        mainPlayer.hackHealth(mem);
                    }

                    if (ammoHack)
                    {
                        mainPlayer.hackAmmo(mem);
                    }

                    if (attackHack)
                    {
                        mainPlayer.hackAttack(mem);
                    }

                    // 마우스 오른쪽 키에 대한 상태를 확인
                    int hotkey = ProcessMemoryReaderApi.GetKeyState(0x02);

                    if (wallHack || (hotkey & 0x8000) != 0)
                    {
                        GetEnemyState(mem); // 적들에 대한 정보 습득
                    }

                    if (wallHack)
                    {
                        overlayForm.hackWall(mainPlayer, enemyPlayer);
                    }

                    
                    if ((hotkey & 0x8000) != 0) // 키가 눌려있다면
                    {
                        float min_err = 100000; // 에러를 굉장히 큰 값으로 초기화
                        float err = 0;
                        double min_x_angle = 0;
                        double min_y_angle = 0;
                        

                        for (int i = 0; i < 30; i++)
                        {
                            // aim hack algorithm
                            
                            err = mainPlayer.getAimErr(mem, enemyPlayer[i].head_x_angle, enemyPlayer[i].head_y_angle);
                            if (min_err > err)
                            {
                                min_err = err;
                                min_x_angle = enemyPlayer[i].head_x_angle;
                                min_y_angle = enemyPlayer[i].head_y_angle;
                            }
                        }
                        
                        mainPlayer.hackAim(mem, min_x_angle, min_y_angle);
                    }

                    // 스페이스에 대한 상태 확인
                    int hotkey2 = ProcessMemoryReaderApi.GetKeyState(0x51);
                    if (superjump && (hotkey2 & 0x8000) != 0) // Q 키를 눌렀다면
                    {
                        mainPlayer.hackjump(mem);
                    }

                    // 데이터 모니터링
                    HealthLBL.Text = "Health: " + mainPlayer.health;    // Health: 100
                    AmmoLBL.Text = "Ammo: " + mainPlayer.ammo;
                    BulletProofLBL.Text = "BulletProof: " + mainPlayer.bullet_proof;
                    AngleLBL.Text = "Angle: " + mainPlayer.x_angle.ToString("#.##") + " | " + mainPlayer.y_angle.ToString("#.##");
                    PositionLBL.Text = "Pos: " + mainPlayer.x_pos.ToString("#.##") + ", " + mainPlayer.y_pos.ToString("#.##") + "," + mainPlayer.z_pos.ToString("#.##");
                    HighBL.Text = "High: " + mainPlayer.y_pos;

                    try
                    {
                        if (comboBox2.SelectedIndex != -1)  // 목록을 선택했다면
                        {
                            int enemyNum = int.Parse(comboBox2.SelectedItem.ToString()); // 1 ~ 30

                            GetEnemyState(mem);

                            EHealthLBL.Text = "Health: " + enemyPlayer[enemyNum - 1].health;
                            EAmmoLBL.Text = "Ammo: " + enemyPlayer[enemyNum - 1].ammo;
                            EBulletProofLBL.Text = "BulletProof: " + enemyPlayer[enemyNum - 1].bullet_proof;
                            EAngleLBL.Text = "Angle: " + enemyPlayer[enemyNum - 1].x_angle.ToString("#.##") + " | " + enemyPlayer[enemyNum - 1].y_angle.ToString("#.##");
                            EPositionLBL.Text = "Pos: " + enemyPlayer[enemyNum - 1].x_pos.ToString("#.##") + ", " + enemyPlayer[enemyNum - 1].y_pos.ToString("#.##") + "," + enemyPlayer[enemyNum - 1].z_pos.ToString("#.##");
                        }
                    }
                    catch (Exception ex) // 예외 처리
                    {
                        MessageBox.Show("적 정보 열람 실패: " + ex.Message);
                    }
                }
                catch { }
            }
        }

        private double GetYDegree(PlayerData mainPlayer, PlayerData enemyPlayer)
        {
            double xz_distance = Math.Sqrt(Math.Pow(mainPlayer.x_pos - enemyPlayer.x_pos, 2) + Math.Pow(mainPlayer.z_pos - enemyPlayer.z_pos, 2));
            double y = mainPlayer.y_pos - enemyPlayer.y_pos;
            double correction = 1;

            if (y > 0)
            {
                correction = -1;
            }
            return correction * Math.Abs( Math.Atan(y / xz_distance) * 180 / Math.PI);
        }

        private double Get2DDegree(PlayerData mainPlayer, PlayerData enemyPlayer)
        {
            double x = mainPlayer.x_pos - enemyPlayer.x_pos;
            double z = mainPlayer.z_pos - enemyPlayer.z_pos;
            double correction = 270;

            if (x < 0) correction = 90;

            return correction + Math.Atan(z / x) * 180 / Math.PI;
        }

        private double GetDistance(PlayerData mainPlayer, PlayerData enemyPlayer)
        {
            //피타고라스의 법칙을 사용해 xz_distance를 먼저 구하기 (2D)
            double xz_distance = Math.Sqrt(Math.Pow(mainPlayer.x_pos- enemyPlayer.x_pos, 2) + Math.Pow(mainPlayer.z_pos - enemyPlayer.z_pos, 2));
            //피타고라스의 법칙을 사용해 distance를 구하기 (3D)
            double distance = Math.Sqrt(Math.Pow(xz_distance, 2) + Math.Pow(mainPlayer.y_pos - enemyPlayer.y_pos, 2));
            return distance;
        }

        private void GetEnemyState(ProcessMemoryReader mem)
        {
            int base_ptr = attachProc.MainModule.BaseAddress.ToInt32() + 0x00583B08;

            for (int i = 0; i < 30; i++)
            {
                int[] offsetArray = { i * 4, 0 }; // 0, 4, 8, 12 총 30명의 플레이어 데이터를 불러옴
                int player_base = mem.ReadMultiLevelPointer(base_ptr, 4, offsetArray);
                enemyPlayer[i] = new PlayerData(player_base);
                enemyPlayer[i].SetPlayerData(mem);
                enemyPlayer[i].distance = GetDistance(mainPlayer, enemyPlayer[i]);
                enemyPlayer[i].head_x_angle = Get2DDegree(mainPlayer, enemyPlayer[i]);
                enemyPlayer[i].head_y_angle = GetYDegree(mainPlayer, enemyPlayer[i]);
            }

            
        }

        private void HealthBT_Click(object sender, EventArgs e)
        {
            if (healthHack)
            {
                healthHack = false;
                HealthHLBL.Text = "동작 안함";
            }
            else
            {
                healthHack = true;
                HealthHLBL.Text = "동작 중";

            }
        }

        private void AmmoBT_Click(object sender, EventArgs e)
        {
            if (ammoHack)
            {
                ammoHack = false;
                ammoHLBL.Text = "동작 안함";
            }
            else
            {
                ammoHack = true;
                ammoHLBL.Text = "동작 중";
            }
        }

        private void WallHackCHB_CheckedChanged(object sender, EventArgs e)
        {
            if (WallHackCHB.Checked == true) // 만약에 체크박스가 체킹되어 있다면
            {
                overlayForm.Show();
                wallHack = true;
            }
            else // 만약에 체크박스가 체킹돼있지 않다면
            {
                overlayForm.Hide();
                wallHack = false;
            }
        }

        // 적의 정보 조작하기
        private void changeEnemyStateBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox2.SelectedIndex != -1) // 목록을 선택했다면
                {
                    int enemyNum = int.Parse(comboBox2.SelectedItem.ToString()); // 1 ~ 30
                    
                    GetEnemyState(mem);

                    // health 변경사항 있다면 변경
                    if (changeEnemyHealth.Text != "")
                        enemyPlayer[enemyNum - 1].changeEnemyHealth(mem, Convert.ToInt32(changeEnemyHealth.Text));

                    // ammo 변경사항 있다면 변경
                    if (changeEnemyAmmo.Text != "")
                        enemyPlayer[enemyNum - 1].changeEnemyAmmo(mem, Convert.ToInt32(changeEnemyAmmo.Text));

                    // bullet proof 변경사항 있다면 변경
                    if (changeEnemyBulletProof.Text != "")
                        enemyPlayer[enemyNum - 1].changeEnemyBulletProof(mem, Convert.ToInt32(changeEnemyBulletProof.Text));
                }
            }
            catch (Exception ex) // 예외 처리
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AttackBT_Click(object sender, EventArgs e)
        {
            if (attackHack)
            {
                attackHack = false;
                AttackHLBL.Text = "동작 안함";
            }
            else
            {
                attackHack = true;
                AttackHLBL.Text = "동작 중";

            }
        }

        // 슈퍼점프 해제하고 원 상태로 돌아오도록 하기
        private void ResetBT_Click(object sender, EventArgs e)
        {
            mainPlayer.reset(mem);
        }

        // 슈퍼점프 활성화
        private void JumpCHB_CheckedChanged(object sender, EventArgs e)
        {
            if (JumpCHB.Checked == true)
            {
                superjump = true;
            }
            else
            {
                superjump = false;
            }
        }

        // 플레이어를 구급상자로 이동시키기
        private void MoveBT_Click(object sender, EventArgs e)
        {
            mainPlayer.hackheal(mem);
        }

        // 플레이어 정보 조작하기
        private void changePlayerStateBtn_Click(object sender, EventArgs e)
        { 
            // health 변경사항 있다면 변경
            if (changePlayerHealth.Text != "")
                mainPlayer.changeEnemyHealth(mem, Convert.ToInt32(changePlayerHealth.Text));

            // ammo 변경사항 있다면 변경
            if (changePlayerAmmo.Text != "")
                mainPlayer.changeEnemyAmmo(mem, Convert.ToInt32(changePlayerAmmo.Text));

            // bullet proof 변경사항 있다면 변경
            if (changePlayerBulletProof.Text != "")
                mainPlayer.changeEnemyBulletProof(mem, Convert.ToInt32(changePlayerBulletProof.Text));
        }
    }
}
