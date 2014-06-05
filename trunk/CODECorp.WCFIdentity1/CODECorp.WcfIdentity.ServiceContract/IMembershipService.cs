using CODECorp.WcfIdentity.DataContract.Messages.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace CODECorp.WcfIdentity.ServiceContract
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IMembershipService : IService
    {

        [OperationContract]
        Task<AddLoginResponse> AddLoginAsync(AddLoginRequest request);

        [OperationContract]
        Task<AddPasswordResponse> AddPasswordAsync(AddPasswordRequest request);

        [OperationContract]
        Task<CreateResponse> CreateAsync(CreateRequest request);

        [OperationContract]
        Task<CreateIdentityResponse> CreateIdentityAsync(CreateIdentityRequest request);

        [OperationContract]
        Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);

        [OperationContract]
        Task<LoginResponse> LoginAsync(LoginRequest request);

        [OperationContract]
        Task<LoginExternalResponse> LoginExternalAsync(LoginExternalRequest request);

        [OperationContract]
        Task<RemoveLoginResponse> RemoveLoginAsync(RemoveLoginRequest request);

        [OperationContract(Name = "FindById")]
        FindByIdResponse FindById(FindByIdRequest request);

        [OperationContract(Name = "FindByIdAsync")]
        Task<FindByIdResponse> FindByIdAsync(FindByIdRequest request);


        [OperationContract(Name = "GetLogins")]
        GetLoginsResponse GetLogins(GetLoginsRequest request);

        [OperationContract(Name = "GetLoginsAsync")]
        Task<GetLoginsResponse> GetLoginsAsync(GetLoginsRequest request);


    }


}
