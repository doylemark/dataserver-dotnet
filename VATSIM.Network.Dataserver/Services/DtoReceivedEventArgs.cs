using System;

namespace VATSIM.Network.Dataserver.Services
{
    public class DtoReceivedEventArgs<T> : EventArgs
    {
        public T Dto { get; }

        public DtoReceivedEventArgs(T dto)
        {
            Dto = dto;
        }
    }
}