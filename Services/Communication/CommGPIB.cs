using Ivi.Visa.Interop;             //Visaライブラリ

namespace GPIBcommunication
{
    public class GPIBComm
    {
        private readonly ResourceManager rm;    //resourceManager
        private static GPIBComm? instance;       //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private GPIBComm()
        {
            //何か初期化する変数等
            rm = new ResourceManager();   // コンストラクタで初期化
        }
        public static GPIBComm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new GPIBComm(); // クラス内でインスタンスを生成
                return instance;
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：NI製GPIBで繋がれた機器のアドレスを抽出
        //コメント
        // フィールド変数deviceInfoにアドレス群を格納
        //*************************************************
        public List<string> GetGPIBList()
        {
            var gpibList = new List<string>();
            if (FindGPIBCon())
                gpibList = GetGPIBAddr();
            return gpibList;
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：NI製GPIBで繋がれた機器のアドレスを抽出
        //コメント
        // フィールド変数deviceInfoにアドレス群を格納
        //*************************************************
        private List<string> GetGPIBAddr()
        {
            var gpibList = new List<string>();
            string[] gpibResources = rm.FindRsrc("GPIB?*INSTR");
            foreach (string resource in gpibResources)
            {
                gpibList.Add(resource); // 例: "GPIB0::5::INSTR"
            }
            return gpibList;
        }

        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：NI製GPIB-USBコントローラ検出
        //*************************************************
        private bool FindGPIBCon()
        {
            try
            {
                string[] resource = rm.FindRsrc("GPIB?*::INTFC");
                return resource.Length > 0;
            }
            catch (Exception)
            {
                //MessageBox.Show($"GPIBコントローラなし");
                return false;
            }
        }
    }
}
