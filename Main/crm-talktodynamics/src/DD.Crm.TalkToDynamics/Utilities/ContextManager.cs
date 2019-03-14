using DD.Crm.TalkToDynamics.Events;
using DD.Crm.TalkToDynamics.Models;
using DD.Crm.TalkToDynamics.Provider;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD.Crm.TalkToDynamics.Utilities
{



    public class ContextManager
    {

        public event ConversationResponseHandler OnMiddleConversationResponse;

        public const string CouldntUnderstand = "No te he entendido";
        public const string OnlyConnectToEnvironment = "Primero necesitas conectarte a un entorno. Dime a qué entorno quieres conectarte";
        public const string SetEntity = "SetEntity";
        public const string SetFilter = "SetFilter";
        public const string UnsetFilter = "UnsetFilter";
        public const string UnsetEntity = "UnsetEntity";
        public const string ConnectToEnvironment = "ConectToEnvironment";
        public const string CouldntFindEnvironmentInAppConfig = "No he encontrado el entorno {0} en la configuración de la aplicación";
        public const string CouldntConnectToEnviroment = "No se ha podido conectar con el entorno {0}. Algún parámetro no es correcto";
        public const string SelectOneEntity = "Por favor, primero selecciona una entidad";
        public const string SelectOneOfAvailableEntities = "Por favor, primero selecciona una entidad disponible en el CRM";
        public const string CouldntFindEntity = "No se ha encontrado la entidad {0} en el CRM";
        public IOrganizationService Service { get; set; }
        public List<EntityMetadata> EntitiesMetadata { get; set; }
        public EntityMetadata SelectedEntity { get; set; }
        public List<QueryExpression> QueryHistory { get; set; }
        public List<ConditionExpression> Filters { get; set; }
        public bool IsConnected { get; set; }

        public string CrmPassword { get; set; }
        public ContextManager(string crmPassword)
        {
            this.CrmPassword = crmPassword;
        }


        public ConversationResponse ProcessInContext(LuisResponse response)
        {
            if (!IsConnected)
            {
                return ConnectPath(response);
            }
            if (SelectedEntity == null)
            {
                return EntityPath(response);
            }
            var unsetEntity = CheckUnsetEntity(response);
            if (unsetEntity != null)
            {
                return unsetEntity;
            }

            var setFilter = CheckSetFilter(response);
            if (setFilter != null)
            {
                return setFilter;
            }

            var unsetFilter = CheckUnsetFilter(response);
            if (unsetFilter != null)
            {
                return unsetFilter;
            }

            return new ConversationResponse("");
        }


        private ConversationResponse CheckUnsetFilter(LuisResponse response)
        {
            if (response.topScoringIntent?.intent == UnsetFilter)
            {
                var cardinal = response.entities.FirstOrDefault(k => k.type == "Cardinal");

                if (cardinal == null)
                {
                    return new ConversationResponse($"Para quitar un filtro indica si deseas que sea el último o todos");
                }
                if (cardinal.entity == "ultimo" || cardinal.entity == "último")
                {
                    var lastFilter = Filters.Last();
                    Filters.Remove(lastFilter);
                }
                else if (cardinal.entity == "todos" || cardinal.entity == "todo" || cardinal.entity == "todas" || cardinal.entity == "toda")
                {
                    Filters.Clear();
                }
                else
                {
                    return new ConversationResponse($"Para quitar un filtro indica si deseas que sea el último o todos");
                }

                var qe = GetBasicQueryExpression(this.SelectedEntity.LogicalName, this.SelectedEntity.PrimaryNameAttribute);
                FilterExpression fe = new FilterExpression();
                foreach (var item in Filters)
                {
                    fe.AddCondition(item);
                }
                qe.Criteria = fe;
                var records = ExecuteQuery(this.Service, qe);
                GetOutputStringOfQuery(records, out string recordsString, out string recordsStrings);
                return new ConversationResponse($"Filtro eliminado correctamente. Se han encontrado {recordsString} {recordsStrings}");

            }
            return null;
        }

        private ConversationResponse CheckSetFilter(LuisResponse response)
        {
            if (response.topScoringIntent?.intent == SetFilter)
            {
                var attribute = response.entities
                    .FirstOrDefault(k => k.type == "FilterAtribute"
                                    || k.type == "ComposedFilterAttribute");
                var comparer = response.entities
                    .FirstOrDefault(k => k.type == "FilterComparator"
                                    || k.type == "ComposedFilterOperator");
                var value = response.entities
                    .FirstOrDefault(k => k.type == "FilterValue"
                                    || k.type == "ComposedFilterValue");

                if (attribute == null || comparer == null || value == null)
                {
                    return new ConversationResponse($"{CouldntUnderstand}");
                }


                var matchesAttributes =
                    GetRelevant<AttributeMetadata>(this.SelectedEntity.Attributes.ToList(), new List<Func<AttributeMetadata, string>>() {
                            (k)=>{ return  k.DisplayName?.UserLocalizedLabel?.Label; },
                        }, attribute.entity);
                if (matchesAttributes.Count == 0)
                {
                    return new ConversationResponse(string.Format("No se ha encontrado el campo {0} de la entidad {1}", attribute.entity, SelectedEntity.DisplayName));
                }
                var attributeMatch = matchesAttributes[0];

                var conditionOperator = GetConditionOperator(comparer.entity, value.entity);
                if (conditionOperator == null)
                {
                    return new ConversationResponse(string.Format("No he entendido el comparador {0}", comparer.entity));
                }

                object objectValue = GetOjbectValue(attributeMatch, (ConditionOperator)conditionOperator, value.entity);

                if (objectValue != null)
                {
                    Filters.Add(new ConditionExpression(attributeMatch.LogicalName, (ConditionOperator)conditionOperator, objectValue));
                }
                else
                {
                    Filters.Add(new ConditionExpression(attributeMatch.LogicalName, (ConditionOperator)conditionOperator));
                }

                var qe = GetBasicQueryExpression(this.SelectedEntity.LogicalName, this.SelectedEntity.PrimaryNameAttribute);
                FilterExpression fe = new FilterExpression();
                foreach (var item in Filters)
                {
                    fe.AddCondition(item);
                }
                qe.Criteria = fe;
                var records = ExecuteQuery(this.Service, qe);
                GetOutputStringOfQuery(records, out string recordsString, out string recordsStrings);

                return new ConversationResponse($"Filtro aplicado correctamente. Se han encontrado {recordsString} {recordsStrings}");
            }
            return null;
        }

        private object GetOjbectValue(AttributeMetadata attributeMetadata, ConditionOperator comparer, string rawValue)
        {
            if (attributeMetadata.AttributeTypeName == "StateType")
            {
                var parsed = (StateAttributeMetadata)attributeMetadata;
                var options = parsed.OptionSet.Options;
                var matchesOptions =
                    GetRelevant(options.ToList(), new List<Func<OptionMetadata, string>>() {
                            (k)=>{ return  ((OptionMetadata)k).Label?.UserLocalizedLabel?.Label; },
                        }, rawValue);
                if (matchesOptions.Count == 0)
                {
                    return null;
                }
                return matchesOptions[0].Value;
            }
            else if (attributeMetadata.AttributeTypeName == "StringType"
                || attributeMetadata.AttributeTypeName == "MemoType")
            {
                return rawValue;
            }
            else if (attributeMetadata.AttributeTypeName == "MultiSelectPicklistType")
            {
                var parsed = (MultiSelectPicklistAttributeMetadata)attributeMetadata;
                var options = parsed.OptionSet.Options;
                var matchesOptions =
                    GetRelevant(options.ToList(), new List<Func<OptionMetadata, string>>() {
                            (k)=>{ return  ((OptionMetadata)k).Label?.UserLocalizedLabel?.Label; },
                        }, rawValue);
                if (matchesOptions.Count == 0)
                {
                    return null;
                }
                return matchesOptions[0].Value;
            }
            else if (attributeMetadata.AttributeTypeName == "PicklistType")
            {
                var parsed = (PicklistAttributeMetadata)attributeMetadata;
                var options = parsed.OptionSet.Options;
                var matchesOptions =
                    GetRelevant(options.ToList(), new List<Func<OptionMetadata, string>>() {
                            (k)=>{ return  ((OptionMetadata)k).Label?.UserLocalizedLabel?.Label; },
                        }, rawValue);
                if (matchesOptions.Count == 0)
                {
                    return null;
                }
                return matchesOptions[0].Value;
            }
            else if (attributeMetadata.AttributeTypeName == "StatusType")
            {
                var parsed = (StatusAttributeMetadata)attributeMetadata;
                var options = parsed.OptionSet.Options;
                var matchesOptions =
                    GetRelevant(options.ToList(), new List<Func<OptionMetadata, string>>() {
                            (k)=>{ return  ((OptionMetadata)k).Label?.UserLocalizedLabel?.Label; },
                        }, rawValue);
                if (matchesOptions.Count == 0)
                {
                    return null;
                }
                return matchesOptions[0].Value;
            }
            else if (attributeMetadata.AttributeTypeName == "DateTimeType")
            {
                if (comparer == ConditionOperator.Today
                    || comparer == ConditionOperator.Yesterday
                    || comparer == ConditionOperator.Tomorrow)
                {
                    return null;
                }
                if (rawValue.ToLower().Contains("hoy"))
                {
                    return DateTime.Today;
                }
                else if (rawValue.ToLower().Contains("mañana") && !rawValue.ToLower().Contains("pasado"))
                {
                    return DateTime.Today.AddDays(1);
                }
                else if (rawValue.ToLower().Contains("mañana") && rawValue.ToLower().Contains("pasado"))
                {
                    return DateTime.Today.AddDays(2);
                }
                else if (rawValue.ToLower().Contains("ayer") && !rawValue.ToLower().Contains("ante"))
                {
                    return DateTime.Today.AddDays(-1);
                }
                else if (rawValue.ToLower().Contains("ayer") && rawValue.ToLower().Contains("ante"))
                {
                    return DateTime.Today.AddDays(-2);
                }
                else
                {
                    throw new NotImplementedException(rawValue);
                }

            }
            else if (attributeMetadata.AttributeTypeName == "IntegerType")
            {
                return int.Parse(rawValue);
            }
            else if (attributeMetadata.AttributeTypeName == "LookupType"
                || attributeMetadata.AttributeTypeName == "OwnerType")
            {
                throw new NotImplementedException("No soporta filtrado por lookups");
            }
            else if (attributeMetadata.AttributeTypeName == "BooleanType")
            {
                if (new string[]
                    {"si", "afirmativo", "afirmativa", "verdadero", "verdadera", "true", "yes", "activo", "activa", "1", "uno", "una" }.ToList().IndexOf(rawValue.ToLower()) > -1)
                {
                    return true;
                }
                else if (new string[]
                    { "no", "negativo", "negativa", "falso", "falsa", "false", "no", "inactivo", "inactiva", "0", "cero" }.ToList().IndexOf(rawValue.ToLower()) > -1)
                {
                    return false;
                }
                return bool.Parse(rawValue);
            }
            else if (attributeMetadata.AttributeTypeName == "DecimalType")
            {
                return decimal.Parse(rawValue);
            }
            else if (attributeMetadata.AttributeTypeName == "MoneyType")
            {
                return decimal.Parse(rawValue);
            }
            return null;
        }

        private static ConditionOperator? GetConditionOperator(string comparer, string value)
        {
            if (comparer == "igual" || comparer == "igual a")
            {
                if (value == "hoy")
                {
                    return ConditionOperator.Today;
                }
                else if (value == "ayer")
                {
                    return ConditionOperator.Yesterday;
                }
                else if (value == "mañana")
                {
                    return ConditionOperator.Tomorrow;
                }
                return ConditionOperator.Equal;
            }
            else if (comparer == "empieza"
                || comparer == "empieza por"
                || comparer == "empiezan por"
                || comparer == "empiezan"
                || comparer == "empiecen"
                || comparer == "empiece"
                || comparer == "empiecen por")
            {
                return ConditionOperator.BeginsWith;
            }
            else if (comparer == "termina por"
                || comparer == "termina"
                || comparer == "acaba"
                || comparer == "acaba por"
                || comparer == "termine por"
                || comparer == "termine"
                || comparer == "acabe"
                || comparer == "acabe por")
            {
                return ConditionOperator.EndsWith;
            }
            else if (comparer == "mayor que")
            {
                return ConditionOperator.GreaterThan;
            }
            else if (comparer == "mayor o igual que" || comparer == "mayor igual que")
            {
                return ConditionOperator.GreaterEqual;
            }
            else if (comparer == "menor que")
            {
                return ConditionOperator.LessThan;
            }
            else if (comparer == "menor o igual que" || comparer == "menor igual que")
            {
                return ConditionOperator.LessThan;
            }
            return null;
        }

        private ConversationResponse CheckUnsetEntity(LuisResponse response)
        {
            if (response.topScoringIntent?.intent == UnsetEntity)
            {
                this.SelectedEntity = null;
                return new ConversationResponse("Seleccione otra entidad");
            }
            return null;
        }

        private ConversationResponse EntityPath(LuisResponse response)
        {
            if (response.topScoringIntent?.intent == SetEntity)
            {
                var entity = response.entities.FirstOrDefault(k => k.type == "EntityName");
                if (entity == null)
                {
                    return new ConversationResponse($"{CouldntUnderstand}. {SelectOneOfAvailableEntities}");
                }
                var entityName = entity.entity;
                var matchesEntities =
                    GetRelevant<EntityMetadata>(this.EntitiesMetadata, new List<Func<EntityMetadata, string>>() {
                            (k)=>{ return  k.DisplayName?.UserLocalizedLabel?.Label; },
                            (k)=>{ return  k.DisplayCollectionName?.UserLocalizedLabel?.Label; },
                        }, entityName);

                if (matchesEntities.Count == 0)
                {
                    return new ConversationResponse(string.Format(CouldntFindEntity, entityName));
                }
                SelectedEntity = matchesEntities[0];

                Filters = new List<ConditionExpression>();

                QueryExpression qe = GetBasicQueryExpression(SelectedEntity.LogicalName, SelectedEntity.PrimaryNameAttribute);
                var records = ExecuteQuery(this.Service, qe);
                GetOutputStringOfQuery(records, out string recordsString, out string recordsStrings);

                return new ConversationResponse($"Conectado correctamente con la entidad {entityName}. Se han encontrado {recordsString} {recordsStrings}");
            }
            else
            {
                return new ConversationResponse(SelectOneEntity);
            }
        }

        private static void GetOutputStringOfQuery(int records, out string recordsString, out string recordsStrings)
        {
            recordsString = records.ToString();
            if (records == 5000)
            {
                recordsString = "más de 5000 ";
            }
            recordsStrings = "registros";
            if (records == 1)
            {
                recordsStrings = "registro";
            }
        }

        private int ExecuteQuery(IOrganizationService service, QueryExpression qe)
        {
            return service.RetrieveMultiple(qe).Entities.Count;
        }

        private QueryExpression GetBasicQueryExpression(string logicalName, string primaryAttributeName)
        {
            QueryExpression qe = new QueryExpression(logicalName);
            qe.ColumnSet = new ColumnSet(primaryAttributeName);
            return qe;
        }

        private List<T> GetRelevant<T>(List<T> list, List<Func<T, string>> propertyAccess, string comparer)
        {
            List<T> output = new List<T>();
            foreach (var item in list)
            {
                foreach (var access in propertyAccess)
                {
                    var stringAccess = access(item);
                    if (!string.IsNullOrEmpty(stringAccess))
                    {
                        if (stringAccess.ToLower() == comparer)
                        {
                            output.Add(item);
                        }
                    }
                }
            }
            if (output.Count > 0)
            {
                return output;
            }
            foreach (var item in list)
            {
                foreach (var access in propertyAccess)
                {
                    var stringAccess = access(item);

                    if (!string.IsNullOrEmpty(stringAccess))
                    {
                        if (stringAccess.ToLower().Contains(comparer))
                        {
                            output.Add(item);
                        }
                    }
                }
            }
            return output;
        }


        private ConversationResponse ConnectPath(LuisResponse response)
        {
            if (response.topScoringIntent?.intent == ConnectToEnvironment)
            {
                var environment = response.entities.FirstOrDefault(k => k.type == "Environment");
                if (environment == null)
                {
                    return new ConversationResponse($"{CouldntUnderstand}. {OnlyConnectToEnvironment}");
                }
                var stringConnection = ConfigManager.GetAppConfig(environment.entity);
                if (string.IsNullOrEmpty(stringConnection))
                {
                    return new ConversationResponse(string.Format(CouldntFindEnvironmentInAppConfig, environment.entity));
                }

                RaiseMiddleConversationResponse($"Intentando conectarse a {environment.entity}");
                string stringConnectionComplete = string.Format(stringConnection, this.CrmPassword);

                IOrganizationService service = CrmDataProvider.GetService(stringConnectionComplete);
                if (service == null)
                {
                    return new ConversationResponse(string.Format(CouldntConnectToEnviroment, environment.entity));
                }
                IsConnected = true;
                this.Service = service;
                RaiseMiddleConversationResponse("Connectado correctamente. Descargando datos de la organización");
                RetrieveAllEntitiesRequest req = new RetrieveAllEntitiesRequest()
                {
                    EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.All,
                    RetrieveAsIfPublished = true
                };
                var responseMetadata = (RetrieveAllEntitiesResponse)service.Execute(req);
                EntitiesMetadata = responseMetadata.EntityMetadata.ToList();

                return new ConversationResponse($"Proceso completado. ¿Qué desea hacer?");
            }
            else
            {
                return new ConversationResponse($"{CouldntUnderstand}. {OnlyConnectToEnvironment}");
            }
        }

        private void RaiseMiddleConversationResponse(string text)
        {
            OnMiddleConversationResponse?
                   .Invoke(this, new ConversationResponseEventArgs(new ConversationResponse(text)));
        }

    }
}
