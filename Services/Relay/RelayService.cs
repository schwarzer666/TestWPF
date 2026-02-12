using TemperatureCharacteristics.Services.Communication;

namespace TemperatureCharacteristics.Services.Relay
{
    public interface IRelayService
    {
        Task<bool> SetRelayOnExclusiveAsync(string serial, int selectedRelay);
        Task<bool> SetAllRelaysOffAsync(string serial);
        Task<bool> SetRelayPortOnAsync(string serial, int port);
        Task<bool> SetRelayPortOffAsync(string serial, int port);
    }
    public class RelayService : IRelayService
    {
        //****************************************************************************
        //動作
        // マニュアルON/OFF用
        // 選択したリレー番号をON、その他をOFFする
        //****************************************************************************
        public async Task<bool> SetRelayOnExclusiveAsync(string serial, int selectedRelay)
        {
            using var bitBang = new FT2232HBitBangService();
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await bitBang.OpenBySerialNumberAsync(serial))
                return false;

            //全ピンOFF
            for (int pin = 0; pin < 6; pin++)
                await bitBang.SetPinAsync(pin, false);

            //選択ピンのみON
            await bitBang.SetPinAsync(selectedRelay - 1, true);
            //bitBang.Dispose();

            return true;
        }
        //****************************************************************************
        //動作
        // マニュアルON/OFF用
        // 全リレーOFF
        //****************************************************************************
        public async Task<bool> SetAllRelaysOffAsync(string serial)
        {
            using var bitBang = new FT2232HBitBangService();
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await bitBang.OpenBySerialNumberAsync(serial))
                return false;

            //全ピンOFF
            for (int pin = 0; pin < 6; pin++)
                await bitBang.SetPinAsync(pin, false);
            //bitBang.Dispose();

            return true;
        }
        //****************************************************************************
        //動作
        // 連続動作用
        // 指定されたリレー番号をON
        //****************************************************************************
        public async Task<bool> SetRelayPortOnAsync(string serial, int port)
        {
            using var bitBang = new FT2232HBitBangService();
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await bitBang.OpenBySerialNumberAsync(serial))
                return false;

            await bitBang.SetPinAsync(port, true);
            //bitBang.Dispose();
            return true;
        }
        //****************************************************************************
        //動作
        // 連続動作用
        // ONしたリレーをOFF
        //****************************************************************************
        public async Task<bool> SetRelayPortOffAsync(string serial, int port)
        {
            using var bitBang = new FT2232HBitBangService();
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await bitBang.OpenBySerialNumberAsync(serial))
                return false;

            await bitBang.SetPinAsync(port, false);
            //bitBang.Dispose();
            return true;
        }
    }
}
