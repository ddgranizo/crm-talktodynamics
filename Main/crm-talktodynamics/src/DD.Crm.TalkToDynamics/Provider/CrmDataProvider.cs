using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Provider
{
    public class CrmDataProvider
    {
        public static IOrganizationService GetService(string stringConnection)
        {
            CrmServiceClient crmService = new CrmServiceClient(stringConnection);
            IOrganizationService serviceProxy = crmService.OrganizationWebProxyClient != null ?
                                                        crmService.OrganizationWebProxyClient :
                                                        (IOrganizationService)crmService.OrganizationServiceProxy;
            return serviceProxy;
        }
    }
}
