using FTD2XX_NET;

namespace TemperatureCharacteristics.Services.Communication
{
    public class FT2232HBitBangService : IDisposable
    {
        private FTDI _ftdi;
        private bool _isOpen = false;
        private readonly object _lock = new object();
        private byte _currentPinState = 0x00;       //現在のピン状態を保持（スレッドセーフ）
        private string? _currentSerialNumber;
        public string? CurrentSerialNumber => _currentSerialNumber;
        public bool IsOpen => _isOpen;

        public async Task<bool> OpenAsync(uint deviceIndex = 0)
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                _ftdi = new FTDI();
                var status = _ftdi.OpenByIndex(deviceIndex);
                if (status != FTDI.FT_STATUS.FT_OK) return false;

                _ftdi.ResetDevice();
                _ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                //BitBangモード(非同期)
                status = _ftdi.SetBitMode(0xFF, 0x01);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    Dispose();
                    return false;
                }

                _isOpen = true;
                return true;
            }
        }
        public async Task<bool> OpenBySerialNumberAsync(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber)) return false;

            lock (_lock)
            {
                if (_isOpen) Dispose(); //前のデバイスを閉じる

                _ftdi = new FTDI();
                var status = _ftdi.OpenBySerialNumber(serialNumber);
                if (status != FTDI.FT_STATUS.FT_OK) return false;

                _ftdi.ResetDevice();
                _ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
                status = _ftdi.SetBitMode(0xFF, 0x01);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    Dispose();
                    return false;
                }

                _currentSerialNumber = serialNumber;
                _isOpen = true;
                return true;
            }
        }

        public async Task<bool> WriteByteAsync(byte data, CancellationToken ct = default)
        {
            if (!_isOpen) return false;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    uint written = 0;
                    var status = _ftdi.Write(new byte[] { data }, 1, ref written);
                    return status == FTDI.FT_STATUS.FT_OK && written == 1;
                }
            }, ct);
        }

        public async Task<byte> ReadByteAsync(CancellationToken ct = default)
        {
            if (!_isOpen) return 0;

            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    byte[] buffer = new byte[1];
                    uint read = 0;
                    var status = _ftdi.Read(buffer, 1, ref read);
                    return status == FTDI.FT_STATUS.FT_OK ? buffer[0] : (byte)0;
                }
            }, ct);
        }
        public void Dispose()
        {
            Close();
        }
        public void Close()
        {
            if (_ftdi != null)
            {
                _ftdi.Close(); //通信終了
                _ftdi = null;//ガベージコレクタによる回収促し
            }
            _isOpen = false;
            _currentSerialNumber = null;
        }
        public async Task<bool> SetPinAsync(int pin, bool state, CancellationToken ct = default)
        {
            if (pin < 0 || pin > 7) return false;
            if (!_isOpen) return false;

            lock (_lock)
            {
                if (state)
                    _currentPinState |= (byte)(1 << pin);
                else
                    _currentPinState &= (byte)~(1 << pin);
            }

            return await WriteByteAsync(_currentPinState, ct);
        }
        public byte GetCurrentPinState()
        {
            lock (_lock)
            {
                return _currentPinState;
            }
        }
    }
}