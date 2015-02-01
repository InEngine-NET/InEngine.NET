using Nest;
using System;

namespace IntegrationEngine.Core.Tests
{
    public class StubElasticClient : IElasticClient
    {
        public StubElasticClient()
        {

        }

        #region IElasticClient implementation

        public IObservable<IReindexResponse<T>> Reindex<T>(Func<ReindexDescriptor<T>, ReindexDescriptor<T>> reindexSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> Scroll<T>(IScrollRequest scrollRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> Scroll<T>(Func<ScrollDescriptor<T>, ScrollDescriptor<T>> scrollSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> ScrollAsync<T>(IScrollRequest scrollRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> ScrollAsync<T>(Func<ScrollDescriptor<T>, ScrollDescriptor<T>> scrollSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IUpdateResponse Update<T>(Func<UpdateDescriptor<T, T>, UpdateDescriptor<T, T>> updateSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IUpdateResponse Update<T, K>(Func<UpdateDescriptor<T, K>, UpdateDescriptor<T, K>> updateSelector) where T : class where K : class
        {
            throw new NotImplementedException();
        }

        public IUpdateResponse Update<T>(IUpdateRequest<T, T> updateRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IUpdateResponse Update<T, K>(IUpdateRequest<T, K> updateRequest) where T : class where K : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpdateResponse> UpdateAsync<T>(Func<UpdateDescriptor<T, T>, UpdateDescriptor<T, T>> updateSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpdateResponse> UpdateAsync<T>(IUpdateRequest<T, T> updateRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpdateResponse> UpdateAsync<T, K>(Func<UpdateDescriptor<T, K>, UpdateDescriptor<T, K>> updateSelector) where T : class where K : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpdateResponse> UpdateAsync<T, K>(IUpdateRequest<T, K> updateRequest) where T : class where K : class
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse UpdateSettings(Func<UpdateSettingsDescriptor, UpdateSettingsDescriptor> updateSettingsSelector)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse UpdateSettings(IUpdateSettingsRequest updateSettingsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> UpdateSettingsAsync(Func<UpdateSettingsDescriptor, UpdateSettingsDescriptor> updateSettingsSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> UpdateSettingsAsync(IUpdateSettingsRequest updateSettingsRequest)
        {
            throw new NotImplementedException();
        }

        public IValidateResponse Validate<T>(Func<ValidateQueryDescriptor<T>, ValidateQueryDescriptor<T>> querySelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IValidateResponse Validate(IValidateQueryRequest validateQueryRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IValidateResponse> ValidateAsync<T>(Func<ValidateQueryDescriptor<T>, ValidateQueryDescriptor<T>> querySelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IValidateResponse> ValidateAsync(IValidateQueryRequest validateQueryRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse OpenIndex(Func<OpenIndexDescriptor, OpenIndexDescriptor> openIndexSelector)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse OpenIndex(IOpenIndexRequest openIndexRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> OpenIndexAsync(Func<OpenIndexDescriptor, OpenIndexDescriptor> openIndexSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> OpenIndexAsync(IOpenIndexRequest openIndexRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse CloseIndex(Func<CloseIndexDescriptor, CloseIndexDescriptor> closeIndexSelector)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse CloseIndex(ICloseIndexRequest closeIndexRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> CloseIndexAsync(Func<CloseIndexDescriptor, CloseIndexDescriptor> closeIndexSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> CloseIndexAsync(ICloseIndexRequest closeIndexRequest)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse Refresh(Func<RefreshDescriptor, RefreshDescriptor> refreshSelector = null)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse Refresh(IRefreshRequest refreshRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> RefreshAsync(Func<RefreshDescriptor, RefreshDescriptor> refreshSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> RefreshAsync(IRefreshRequest refreshRequest)
        {
            throw new NotImplementedException();
        }

        public ISegmentsResponse Segments(Func<SegmentsDescriptor, SegmentsDescriptor> segmentsSelector = null)
        {
            throw new NotImplementedException();
        }

        public ISegmentsResponse Segments(ISegmentsRequest segmentsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISegmentsResponse> SegmentsAsync(Func<SegmentsDescriptor, SegmentsDescriptor> segmentsSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISegmentsResponse> SegmentsAsync(ISegmentsRequest segmentsRequest)
        {
            throw new NotImplementedException();
        }

        public IClusterStateResponse ClusterState(Func<ClusterStateDescriptor, ClusterStateDescriptor> clusterStateSelector = null)
        {
            throw new NotImplementedException();
        }

        public IClusterStateResponse ClusterState(IClusterStateRequest clusterStateRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterStateResponse> ClusterStateAsync(Func<ClusterStateDescriptor, ClusterStateDescriptor> clusterStateSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterStateResponse> ClusterStateAsync(IClusterStateRequest clusterStateRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse PutWarmer(string name, Func<PutWarmerDescriptor, PutWarmerDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse PutWarmer(IPutWarmerRequest putWarmerRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> PutWarmerAsync(string name, Func<PutWarmerDescriptor, PutWarmerDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> PutWarmerAsync(IPutWarmerRequest putWarmerRequest)
        {
            throw new NotImplementedException();
        }

        public IWarmerResponse GetWarmer(string name, Func<GetWarmerDescriptor, GetWarmerDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IWarmerResponse GetWarmer(IGetWarmerRequest getWarmerRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IWarmerResponse> GetWarmerAsync(string name, Func<GetWarmerDescriptor, GetWarmerDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IWarmerResponse> GetWarmerAsync(IGetWarmerRequest getWarmerRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse DeleteWarmer(string name, Func<DeleteWarmerDescriptor, DeleteWarmerDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse DeleteWarmer(IDeleteWarmerRequest deleteWarmerRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> DeleteWarmerAsync(string name, Func<DeleteWarmerDescriptor, DeleteWarmerDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> DeleteWarmerAsync(IDeleteWarmerRequest deleteWarmerRequest)
        {
            throw new NotImplementedException();
        }

        public ITemplateResponse GetTemplate(string name, Func<GetTemplateDescriptor, GetTemplateDescriptor> getTemplateSelector = null)
        {
            throw new NotImplementedException();
        }

        public ITemplateResponse GetTemplate(IGetTemplateRequest getTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ITemplateResponse> GetTemplateAsync(string name, Func<GetTemplateDescriptor, GetTemplateDescriptor> getTemplateSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ITemplateResponse> GetTemplateAsync(IGetTemplateRequest getTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse PutTemplate(string name, Func<PutTemplateDescriptor, PutTemplateDescriptor> putTemplateSelector)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse PutTemplate(IPutTemplateRequest putTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> PutTemplateAsync(string name, Func<PutTemplateDescriptor, PutTemplateDescriptor> putTemplateSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> PutTemplateAsync(IPutTemplateRequest putTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse DeleteTemplate(string name, Func<DeleteTemplateDescriptor, DeleteTemplateDescriptor> deleteTemplateSelector = null)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse DeleteTemplate(IDeleteTemplateRequest deleteTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> DeleteTemplateAync(string name, Func<DeleteTemplateDescriptor, DeleteTemplateDescriptor> deleteTemplateSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> DeleteTemplateAync(IDeleteTemplateRequest deleteTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> DeleteTemplateAsync(string name, Func<DeleteTemplateDescriptor, DeleteTemplateDescriptor> deleteTemplateSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> DeleteTemplateAsync(IDeleteTemplateRequest deleteTemplateRequest)
        {
            throw new NotImplementedException();
        }

        public IUnregisterPercolateResponse UnregisterPercolator<T>(string name, Func<UnregisterPercolatorDescriptor<T>, UnregisterPercolatorDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public IUnregisterPercolateResponse UnregisterPercolator(IUnregisterPercolatorRequest unregisterPercolatorRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUnregisterPercolateResponse> UnregisterPercolatorAsync<T>(string name, Func<UnregisterPercolatorDescriptor<T>, UnregisterPercolatorDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUnregisterPercolateResponse> UnregisterPercolatorAsync(IUnregisterPercolatorRequest unregisterPercolatorRequest)
        {
            throw new NotImplementedException();
        }

        public IRegisterPercolateResponse RegisterPercolator<T>(string name, Func<RegisterPercolatorDescriptor<T>, RegisterPercolatorDescriptor<T>> percolatorSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IRegisterPercolateResponse RegisterPercolator(IRegisterPercolatorRequest registerPercolatorRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRegisterPercolateResponse> RegisterPercolatorAsync<T>(string name, Func<RegisterPercolatorDescriptor<T>, RegisterPercolatorDescriptor<T>> percolatorSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRegisterPercolateResponse> RegisterPercolatorAsync(IRegisterPercolatorRequest registerPercolatorRequest)
        {
            throw new NotImplementedException();
        }

        public IPercolateResponse Percolate<T>(Func<PercolateDescriptor<T>, PercolateDescriptor<T>> percolateSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IPercolateResponse Percolate<T>(IPercolateRequest<T> percolateRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPercolateResponse> PercolateAsync<T>(Func<PercolateDescriptor<T>, PercolateDescriptor<T>> percolateSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPercolateResponse> PercolateAsync<T>(IPercolateRequest<T> percolateRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IPercolateCountResponse PercolateCount<T>(T @object, Func<PercolateCountDescriptor<T>, PercolateCountDescriptor<T>> percolateSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public IPercolateCountResponse PercolateCount<T>(Func<PercolateCountDescriptor<T>, PercolateCountDescriptor<T>> percolateSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IPercolateCountResponse PercolateCount<T>(IPercolateCountRequest<T> percolateCountRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPercolateCountResponse> PercolateCountAsync<T>(T @object, Func<PercolateCountDescriptor<T>, PercolateCountDescriptor<T>> percolateSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPercolateCountResponse> PercolateCountAsync<T>(Func<PercolateCountDescriptor<T>, PercolateCountDescriptor<T>> percolateSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPercolateCountResponse> PercolateCountAsync<T>(IPercolateCountRequest<T> percolateCountRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IIndicesResponse Map<T>(Func<PutMappingDescriptor<T>, PutMappingDescriptor<T>> mappingSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IIndicesResponse Map(IPutMappingRequest putMappingRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesResponse> MapAsync<T>(Func<PutMappingDescriptor<T>, PutMappingDescriptor<T>> mappingSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesResponse> MapAsync(IPutMappingRequest putMappingRequest)
        {
            throw new NotImplementedException();
        }

        public IGetMappingResponse GetMapping<T>(Func<GetMappingDescriptor<T>, GetMappingDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public IGetMappingResponse GetMapping(IGetMappingRequest getMappingRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetMappingResponse> GetMappingAsync<T>(Func<GetMappingDescriptor<T>, GetMappingDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetMappingResponse> GetMappingAsync(IGetMappingRequest getMappingRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesResponse DeleteMapping<T>(Func<DeleteMappingDescriptor<T>, DeleteMappingDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public IIndicesResponse DeleteMapping(IDeleteMappingRequest deleteMappingRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesResponse> DeleteMappingAsync<T>(Func<DeleteMappingDescriptor<T>, DeleteMappingDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesResponse> DeleteMappingAsync(IDeleteMappingRequest deleteMappingRequest)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse Flush(Func<FlushDescriptor, FlushDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse Flush(IFlushRequest flushRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> FlushAsync(Func<FlushDescriptor, FlushDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> FlushAsync(IFlushRequest flushRequest)
        {
            throw new NotImplementedException();
        }

        public IIndexSettingsResponse GetIndexSettings(Func<GetIndexSettingsDescriptor, GetIndexSettingsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IIndexSettingsResponse GetIndexSettings(IGetIndexSettingsRequest getIndexSettingsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndexSettingsResponse> GetIndexSettingsAsync(Func<GetIndexSettingsDescriptor, GetIndexSettingsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndexSettingsResponse> GetIndexSettingsAsync(IGetIndexSettingsRequest getIndexSettingsRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesResponse DeleteIndex(Func<DeleteIndexDescriptor, DeleteIndexDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IIndicesResponse DeleteIndex(IDeleteIndexRequest deleteIndexRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesResponse> DeleteIndexAsync(Func<DeleteIndexDescriptor, DeleteIndexDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesResponse> DeleteIndexAsync(IDeleteIndexRequest deleteIndexRequest)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse ClearCache(Func<ClearCacheDescriptor, ClearCacheDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse ClearCache(IClearCacheRequest clearCacheRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> ClearCacheAsync(Func<ClearCacheDescriptor, ClearCacheDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> ClearCacheAsync(IClearCacheRequest clearCacheRequest)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse CreateIndex(Func<CreateIndexDescriptor, CreateIndexDescriptor> createIndexSelector)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse CreateIndex(ICreateIndexRequest createIndexRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> CreateIndexAsync(Func<CreateIndexDescriptor, CreateIndexDescriptor> createIndexSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> CreateIndexAsync(ICreateIndexRequest createIndexRequest)
        {
            throw new NotImplementedException();
        }

        public IRootInfoResponse RootNodeInfo(Func<InfoDescriptor, InfoDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IRootInfoResponse RootNodeInfo(IInfoRequest infoRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRootInfoResponse> RootNodeInfoAsync(Func<InfoDescriptor, InfoDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRootInfoResponse> RootNodeInfoAsync(IInfoRequest infoRequest)
        {
            throw new NotImplementedException();
        }

        public IGlobalStatsResponse IndicesStats(Func<IndicesStatsDescriptor, IndicesStatsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IGlobalStatsResponse IndicesStats(IIndicesStatsRequest indicesStatsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGlobalStatsResponse> IndicesStatsAsync(Func<IndicesStatsDescriptor, IndicesStatsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGlobalStatsResponse> IndicesStatsAsync(IIndicesStatsRequest indicesStatsRequest)
        {
            throw new NotImplementedException();
        }

        public INodeInfoResponse NodesInfo(Func<NodesInfoDescriptor, NodesInfoDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public INodeInfoResponse NodesInfo(INodesInfoRequest nodesInfoRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodeInfoResponse> NodesInfoAsync(Func<NodesInfoDescriptor, NodesInfoDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodeInfoResponse> NodesInfoAsync(INodesInfoRequest nodesInfoRequest)
        {
            throw new NotImplementedException();
        }

        public INodeStatsResponse NodesStats(Func<NodesStatsDescriptor, NodesStatsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public INodeStatsResponse NodesStats(INodesStatsRequest nodesStatsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodeStatsResponse> NodesStatsAsync(Func<NodesStatsDescriptor, NodesStatsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodeStatsResponse> NodesStatsAsync(INodesStatsRequest nodesStatsRequest)
        {
            throw new NotImplementedException();
        }

        public INodesHotThreadsResponse NodesHotThreads(Func<NodesHotThreadsDescriptor, NodesHotThreadsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public INodesHotThreadsResponse NodesHotThreads(INodesHotThreadsRequest nodesHotThreadsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodesHotThreadsResponse> NodesHotThreadsAsync(Func<NodesHotThreadsDescriptor, NodesHotThreadsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodesHotThreadsResponse> NodesHotThreadsAsync(INodesHotThreadsRequest nodesHotThreadsRequest)
        {
            throw new NotImplementedException();
        }

        public INodesShutdownResponse NodesShutdown(Func<NodesShutdownDescriptor, NodesShutdownDescriptor> nodesShutdownSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodesShutdownResponse> NodesShutdownAsync(Func<NodesShutdownDescriptor, NodesShutdownDescriptor> nodesShutdownSelector = null)
        {
            throw new NotImplementedException();
        }

        public INodesShutdownResponse NodesShutdown(INodesShutdownRequest nodesShutdownRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<INodesShutdownResponse> NodesShutdownAsync(INodesShutdownRequest nodesShutdownRequest)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse IndexExists(Func<IndexExistsDescriptor, IndexExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse IndexExists(IIndexExistsRequest indexExistsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> IndexExistsAsync(Func<IndexExistsDescriptor, IndexExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> IndexExistsAsync(IIndexExistsRequest indexExistsRequest)
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> MoreLikeThis<T>(Func<MoreLikeThisDescriptor<T>, MoreLikeThisDescriptor<T>> mltSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> MoreLikeThis<T>(IMoreLikeThisRequest moreLikeThisRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> MoreLikeThisAsync<T>(Func<MoreLikeThisDescriptor<T>, MoreLikeThisDescriptor<T>> mltSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> MoreLikeThisAsync<T>(IMoreLikeThisRequest moreLikeThisRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IHealthResponse ClusterHealth(Func<ClusterHealthDescriptor, ClusterHealthDescriptor> clusterHealthSelector = null)
        {
            throw new NotImplementedException();
        }

        public IHealthResponse ClusterHealth(IClusterHealthRequest clusterHealthRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IHealthResponse> ClusterHealthAsync(Func<ClusterHealthDescriptor, ClusterHealthDescriptor> clusterHealthSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IHealthResponse> ClusterHealthAsync(IClusterHealthRequest clusterHealthRequest)
        {
            throw new NotImplementedException();
        }

        public IClusterStatsResponse ClusterStats(Func<ClusterStatsDescriptor, ClusterStatsDescriptor> clusterStatsSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterStatsResponse> ClusterStatsAsync(Func<ClusterStatsDescriptor, ClusterStatsDescriptor> clusterStatsSelector = null)
        {
            throw new NotImplementedException();
        }

        public IClusterStatsResponse ClusterStats(IClusterStatsRequest clusterStatsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterStatsResponse> ClusterStatsAsync(IClusterStatsRequest clusterStatsRequest)
        {
            throw new NotImplementedException();
        }

        public IClusterRerouteResponse ClusterReroute(Func<ClusterRerouteDescriptor, ClusterRerouteDescriptor> clusterRerouteSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterRerouteResponse> ClusterRerouteAsync(Func<ClusterRerouteDescriptor, ClusterRerouteDescriptor> clusterRerouteSelector)
        {
            throw new NotImplementedException();
        }

        public IClusterRerouteResponse ClusterReroute(IClusterRerouteRequest clusterRerouteRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterRerouteResponse> ClusterRerouteAsync(IClusterRerouteRequest clusterRerouteRequest)
        {
            throw new NotImplementedException();
        }

        public IAnalyzeResponse Analyze(Func<AnalyzeDescriptor, AnalyzeDescriptor> analyzeSelector)
        {
            throw new NotImplementedException();
        }

        public IAnalyzeResponse Analyze(IAnalyzeRequest analyzeRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAnalyzeResponse> AnalyzeAsync(Func<AnalyzeDescriptor, AnalyzeDescriptor> analyzeSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAnalyzeResponse> AnalyzeAsync(IAnalyzeRequest analyzeRequest)
        {
            throw new NotImplementedException();
        }

        public virtual ISearchResponse<T> Search<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> Search<T>(ISearchRequest request) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<TResult> Search<T, TResult>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchSelector) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<TResult> Search<T, TResult>(ISearchRequest request) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> SearchAsync<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> SearchAsync<T>(ISearchRequest request) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<TResult>> SearchAsync<T, TResult>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchSelector) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<TResult>> SearchAsync<T, TResult>(ISearchRequest request) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> SearchTemplate<T>(Func<SearchTemplateDescriptor<T>, SearchTemplateDescriptor<T>> selector) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<TResult> SearchTemplate<T, TResult>(Func<SearchTemplateDescriptor<T>, SearchTemplateDescriptor<T>> selector) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<T> SearchTemplate<T>(ISearchTemplateRequest request) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchResponse<TResult> SearchTemplate<T, TResult>(ISearchTemplateRequest request) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> SearchTemplateAsync<T>(Func<SearchTemplateDescriptor<T>, SearchTemplateDescriptor<T>> selector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<TResult>> SearchTemplateAsync<T, TResult>(Func<SearchTemplateDescriptor<T>, SearchTemplateDescriptor<T>> selector) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<T>> SearchTemplateAsync<T>(ISearchTemplateRequest request) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchResponse<TResult>> SearchTemplateAsync<T, TResult>(ISearchTemplateRequest request) where T : class where TResult : class
        {
            throw new NotImplementedException();
        }

        public IGetSearchTemplateResponse GetSearchTemplate(string name, Func<GetSearchTemplateDescriptor, GetSearchTemplateDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IGetSearchTemplateResponse GetSearchTemplate(IGetSearchTemplateRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetSearchTemplateResponse> GetSearchTemplateAsync(string name, Func<GetSearchTemplateDescriptor, GetSearchTemplateDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetSearchTemplateResponse> GetSearchTemplateAsync(IGetSearchTemplateRequest request)
        {
            throw new NotImplementedException();
        }

        public IPutSearchTemplateResponse PutSearchTemplate(string name, Func<PutSearchTemplateDescriptor, PutSearchTemplateDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IPutSearchTemplateResponse PutSearchTemplate(IPutSearchTemplateRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPutSearchTemplateResponse> PutSearchTemplateAsync(string name, Func<PutSearchTemplateDescriptor, PutSearchTemplateDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPutSearchTemplateResponse> PutSearchTemplateAsync(IPutSearchTemplateRequest request)
        {
            throw new NotImplementedException();
        }

        public IDeleteSearchTemplateResponse DeleteSearchTemplate(string name, Func<DeleteSearchTemplateDescriptor, DeleteSearchTemplateDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IDeleteSearchTemplateResponse DeleteSearchTemplate(IDeleteSearchTemplateRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteSearchTemplateResponse> DeleteSearchTemplateAsync(string name, Func<DeleteSearchTemplateDescriptor, DeleteSearchTemplateDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteSearchTemplateResponse> DeleteSearchTemplateAsync(IDeleteSearchTemplateRequest request)
        {
            throw new NotImplementedException();
        }

        public IMultiSearchResponse MultiSearch(Func<MultiSearchDescriptor, MultiSearchDescriptor> multiSearchSelector)
        {
            throw new NotImplementedException();
        }

        public IMultiSearchResponse MultiSearch(IMultiSearchRequest multiSearchRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiSearchResponse> MultiSearchAsync(Func<MultiSearchDescriptor, MultiSearchDescriptor> multiSearchSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiSearchResponse> MultiSearchAsync(IMultiSearchRequest multiSearchRequest)
        {
            throw new NotImplementedException();
        }

        public ICountResponse Count<T>(Func<CountDescriptor<T>, CountDescriptor<T>> countSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public ICountResponse Count<T>(ICountRequest countRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICountResponse> CountAsync<T>(Func<CountDescriptor<T>, CountDescriptor<T>> countSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICountResponse> CountAsync<T>(ICountRequest countRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IDeleteResponse DeleteByQuery<T>(Func<DeleteByQueryDescriptor<T>, DeleteByQueryDescriptor<T>> deleteByQuerySelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IDeleteResponse DeleteByQuery(IDeleteByQueryRequest deleteByQueryRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteResponse> DeleteByQueryAsync<T>(Func<DeleteByQueryDescriptor<T>, DeleteByQueryDescriptor<T>> deleteByQuerySelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteResponse> DeleteByQueryAsync(IDeleteByQueryRequest deleteByQueryRequest)
        {
            throw new NotImplementedException();
        }

        public IBulkResponse Bulk(IBulkRequest bulkRequest)
        {
            throw new NotImplementedException();
        }

        public IBulkResponse Bulk(Func<BulkDescriptor, BulkDescriptor> bulkSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IBulkResponse> BulkAsync(IBulkRequest bulkRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IBulkResponse> BulkAsync(Func<BulkDescriptor, BulkDescriptor> bulkSelector)
        {
            throw new NotImplementedException();
        }

        public virtual IIndexResponse Index<T>(T @object, Func<IndexDescriptor<T>, IndexDescriptor<T>> indexSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IIndexResponse Index<T>(IIndexRequest<T> indexRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndexResponse> IndexAsync<T>(T @object, Func<IndexDescriptor<T>, IndexDescriptor<T>> indexSelector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndexResponse> IndexAsync<T>(IIndexRequest<T> indexRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IDeleteResponse Delete<T>(Func<DeleteDescriptor<T>, DeleteDescriptor<T>> deleteSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IDeleteResponse Delete(IDeleteRequest deleteRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteResponse> DeleteAsync<T>(Func<DeleteDescriptor<T>, DeleteDescriptor<T>> deleteSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteResponse> DeleteAsync(IDeleteRequest deleteRequest)
        {
            throw new NotImplementedException();
        }

        public IMultiGetResponse MultiGet(Func<MultiGetDescriptor, MultiGetDescriptor> multiGetSelector)
        {
            throw new NotImplementedException();
        }

        public IMultiGetResponse MultiGet(IMultiGetRequest multiGetRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiGetResponse> MultiGetAsync(Func<MultiGetDescriptor, MultiGetDescriptor> multiGetSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiGetResponse> MultiGetAsync(IMultiGetRequest multiGetRequest)
        {
            throw new NotImplementedException();
        }

        public T Source<T>(Func<SourceDescriptor<T>, SourceDescriptor<T>> getSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public T Source<T>(ISourceRequest sourceRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<T> SourceAsync<T>(Func<SourceDescriptor<T>, SourceDescriptor<T>> getSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<T> SourceAsync<T>(ISourceRequest sourceRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IGetResponse<T> Get<T>(Func<GetDescriptor<T>, GetDescriptor<T>> getSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IGetResponse<T> Get<T>(IGetRequest getRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetResponse<T>> GetAsync<T>(Func<GetDescriptor<T>, GetDescriptor<T>> getSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetResponse<T>> GetAsync<T>(IGetRequest getRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse Alias(Func<AliasDescriptor, AliasDescriptor> aliasSelector)
        {
            throw new NotImplementedException();
        }

        public IIndicesOperationResponse Alias(IAliasRequest aliasRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> AliasAsync(Func<AliasDescriptor, AliasDescriptor> aliasSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IIndicesOperationResponse> AliasAsync(IAliasRequest aliasRequest)
        {
            throw new NotImplementedException();
        }

        public IGetAliasesResponse GetAlias(Func<GetAliasDescriptor, GetAliasDescriptor> getAliasDescriptor)
        {
            throw new NotImplementedException();
        }

        public IGetAliasesResponse GetAlias(IGetAliasRequest getAliasRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetAliasesResponse> GetAliasAsync(Func<GetAliasDescriptor, GetAliasDescriptor> getAliasDescriptor)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetAliasesResponse> GetAliasAsync(IGetAliasRequest getAliasRequest)
        {
            throw new NotImplementedException();
        }

        public IGetAliasesResponse GetAliases(Func<GetAliasesDescriptor, GetAliasesDescriptor> getAliasesDescriptor)
        {
            throw new NotImplementedException();
        }

        public IGetAliasesResponse GetAliases(IGetAliasesRequest getAliasesRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetAliasesResponse> GetAliasesAsync(Func<GetAliasesDescriptor, GetAliasesDescriptor> getAliasesDescriptor)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetAliasesResponse> GetAliasesAsync(IGetAliasesRequest getAliasesRequest)
        {
            throw new NotImplementedException();
        }

        public IPutAliasResponse PutAlias(IPutAliasRequest putAliasRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPutAliasResponse> PutAliasAsync(IPutAliasRequest putAliasRequest)
        {
            throw new NotImplementedException();
        }

        public IPutAliasResponse PutAlias(Func<PutAliasDescriptor, PutAliasDescriptor> putAliasDescriptor)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPutAliasResponse> PutAliasAsync(Func<PutAliasDescriptor, PutAliasDescriptor> putAliasDescriptor)
        {
            throw new NotImplementedException();
        }

        public IDeleteAliasResponse DeleteAlias(IDeleteAliasRequest deleteAliasRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteAliasResponse> DeleteAliasAsync(IDeleteAliasRequest deleteAliasRequest)
        {
            throw new NotImplementedException();
        }

        public IDeleteAliasResponse DeleteAlias<T>(Func<DeleteAliasDescriptor<T>, DeleteAliasDescriptor<T>> deleteAliasDescriptor) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteAliasResponse> DeleteAliasAsync<T>(Func<DeleteAliasDescriptor<T>, DeleteAliasDescriptor<T>> deleteAliasDescriptor) where T : class
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse Optimize(Func<OptimizeDescriptor, OptimizeDescriptor> optimizeSelector = null)
        {
            throw new NotImplementedException();
        }

        public IShardsOperationResponse Optimize(IOptimizeRequest optimizeRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> OptimizeAsync(Func<OptimizeDescriptor, OptimizeDescriptor> optimizeSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IShardsOperationResponse> OptimizeAsync(IOptimizeRequest optimizeRequest)
        {
            throw new NotImplementedException();
        }

        public IStatusResponse Status(Func<IndicesStatusDescriptor, IndicesStatusDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IStatusResponse Status(IIndicesStatusRequest statusRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IStatusResponse> StatusAsync(Func<IndicesStatusDescriptor, IndicesStatusDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IStatusResponse> StatusAsync(IIndicesStatusRequest statusRequest)
        {
            throw new NotImplementedException();
        }

        public ITermVectorResponse TermVector<T>(Func<TermvectorDescriptor<T>, TermvectorDescriptor<T>> termVectorSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public ITermVectorResponse TermVector(ITermvectorRequest termvectorRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ITermVectorResponse> TermVectorAsync<T>(Func<TermvectorDescriptor<T>, TermvectorDescriptor<T>> termVectorSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ITermVectorResponse> TermVectorAsync(ITermvectorRequest termvectorRequest)
        {
            throw new NotImplementedException();
        }

        public IMultiTermVectorResponse MultiTermVectors<T>(Func<MultiTermVectorsDescriptor<T>, MultiTermVectorsDescriptor<T>> multiTermVectorsSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IMultiTermVectorResponse MultiTermVectors(IMultiTermVectorsRequest multiTermVectorsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiTermVectorResponse> MultiTermVectorsAsync<T>(Func<MultiTermVectorsDescriptor<T>, MultiTermVectorsDescriptor<T>> multiTermVectorsSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiTermVectorResponse> MultiTermVectorsAsync(IMultiTermVectorsRequest multiTermVectorsRequest)
        {
            throw new NotImplementedException();
        }

        public ISuggestResponse Suggest<T>(Func<SuggestDescriptor<T>, SuggestDescriptor<T>> selector) where T : class
        {
            throw new NotImplementedException();
        }

        public ISuggestResponse Suggest(ISuggestRequest suggestRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISuggestResponse> SuggestAsync<T>(Func<SuggestDescriptor<T>, SuggestDescriptor<T>> selector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISuggestResponse> SuggestAsync(ISuggestRequest suggestRequest)
        {
            throw new NotImplementedException();
        }

        public IEmptyResponse ClearScroll(Func<ClearScrollDescriptor, ClearScrollDescriptor> clearScrollSelector)
        {
            throw new NotImplementedException();
        }

        public IEmptyResponse ClearScroll(IClearScrollRequest clearScrollRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IEmptyResponse> ClearScrollAsync(Func<ClearScrollDescriptor, ClearScrollDescriptor> clearScrollSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IEmptyResponse> ClearScrollAsync(IClearScrollRequest clearScrollRequest)
        {
            throw new NotImplementedException();
        }

        public virtual IExistsResponse DocumentExists<T>(Func<DocumentExistsDescriptor<T>, DocumentExistsDescriptor<T>> existsSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IExistsResponse DocumentExists(IDocumentExistsRequest documentExistsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> DocumentExistsAsync<T>(Func<DocumentExistsDescriptor<T>, DocumentExistsDescriptor<T>> existsSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> DocumentExistsAsync(IDocumentExistsRequest documentExistsRequest)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse CreateRepository(string repository, Func<CreateRepositoryDescriptor, CreateRepositoryDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse CreateRepository(ICreateRepositoryRequest createRepositoryRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> CreateRepositoryAsync(string repository, Func<CreateRepositoryDescriptor, CreateRepositoryDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> CreateRepositoryAsync(ICreateRepositoryRequest createRepositoryRequest)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse DeleteRepository(string repository, Func<DeleteRepositoryDescriptor, DeleteRepositoryDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse DeleteRepository(IDeleteRepositoryRequest deleteRepositoryRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> DeleteRepositoryAsync(string repository, Func<DeleteRepositoryDescriptor, DeleteRepositoryDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> DeleteRepositoryAsync(IDeleteRepositoryRequest deleteRepositoryRequest)
        {
            throw new NotImplementedException();
        }

        public ISnapshotResponse Snapshot(string repository, string snapshotName, Func<SnapshotDescriptor, SnapshotDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ISnapshotResponse Snapshot(ISnapshotRequest snapshotRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISnapshotResponse> SnapshotAsync(string repository, string snapshotName, Func<SnapshotDescriptor, SnapshotDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISnapshotResponse> SnapshotAsync(ISnapshotRequest snapshotRequest)
        {
            throw new NotImplementedException();
        }

        public IObservable<ISnapshotStatusResponse> SnapshotObservable(TimeSpan interval, Func<SnapshotDescriptor, SnapshotDescriptor> snapshotSelector = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<ISnapshotStatusResponse> SnapshotObservable(TimeSpan interval, ISnapshotRequest snapshotRequest)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse DeleteSnapshot(string repository, string snapshotName, Func<DeleteSnapshotDescriptor, DeleteSnapshotDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IAcknowledgedResponse DeleteSnapshot(IDeleteSnapshotRequest deleteSnapshotRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> DeleteSnapshotAsync(string repository, string snapshotName, Func<DeleteSnapshotDescriptor, DeleteSnapshotDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IAcknowledgedResponse> DeleteSnapshotAsync(IDeleteSnapshotRequest deleteSnapshotRequest)
        {
            throw new NotImplementedException();
        }

        public IGetSnapshotResponse GetSnapshot(string repository, string snapshotName, Func<GetSnapshotDescriptor, GetSnapshotDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IGetSnapshotResponse GetSnapshot(IGetSnapshotRequest getSnapshotRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetSnapshotResponse> GetSnapshotAsync(string repository, string snapshotName, Func<GetSnapshotDescriptor, GetSnapshotDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetSnapshotResponse> GetSnapshotAsync(IGetSnapshotRequest getSnapshotRequest)
        {
            throw new NotImplementedException();
        }

        public IRestoreResponse Restore(string repository, string snapshotName, Func<RestoreDescriptor, RestoreDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IRestoreResponse Restore(IRestoreRequest restoreRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRestoreResponse> RestoreAsync(string repository, string snapshotName, Func<RestoreDescriptor, RestoreDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRestoreResponse> RestoreAsync(IRestoreRequest restoreRequest)
        {
            throw new NotImplementedException();
        }

        public IObservable<IRecoveryStatusResponse> RestoreObservable(TimeSpan interval, Func<RestoreDescriptor, RestoreDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<IRecoveryStatusResponse> RestoreObservable(TimeSpan interval, IRestoreRequest restoreRequest)
        {
            throw new NotImplementedException();
        }

        public IClusterPutSettingsResponse ClusterSettings(Func<ClusterSettingsDescriptor, ClusterSettingsDescriptor> clusterHealthSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterPutSettingsResponse> ClusterSettingsAsync(Func<ClusterSettingsDescriptor, ClusterSettingsDescriptor> clusterHealthSelector)
        {
            throw new NotImplementedException();
        }

        public IClusterPutSettingsResponse ClusterSettings(IClusterSettingsRequest clusterSettingsRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterPutSettingsResponse> ClusterSettingsAsync(IClusterSettingsRequest clusterSettingsRequest)
        {
            throw new NotImplementedException();
        }

        public IClusterGetSettingsResponse ClusterGetSettings(Func<ClusterGetSettingsDescriptor, ClusterGetSettingsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterGetSettingsResponse> ClusterGetSettingsAsync(Func<ClusterGetSettingsDescriptor, ClusterGetSettingsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IClusterGetSettingsResponse ClusterGetSettings(IClusterGetSettingsRequest clusterSettingsRequest = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterGetSettingsResponse> ClusterGetSettingsAsync(IClusterGetSettingsRequest clusterSettingsRequest = null)
        {
            throw new NotImplementedException();
        }

        public IClusterPendingTasksResponse ClusterPendingTasks(Func<ClusterPendingTasksDescriptor, ClusterPendingTasksDescriptor> pendingTasksSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterPendingTasksResponse> ClusterPendingTasksAsync(Func<ClusterPendingTasksDescriptor, ClusterPendingTasksDescriptor> pendingTasksSelector = null)
        {
            throw new NotImplementedException();
        }

        public IClusterPendingTasksResponse ClusterPendingTasks(IClusterPendingTasksRequest pendingTasksRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IClusterPendingTasksResponse> ClusterPendingTasksAsync(IClusterPendingTasksRequest pendingTasksRequest)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse AliasExists(Func<AliasExistsDescriptor, AliasExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse AliasExists(IAliasExistsRequest AliasRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> AliasExistsAsync(Func<AliasExistsDescriptor, AliasExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> AliasExistsAsync(IAliasExistsRequest AliasRequest)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse TypeExists(Func<TypeExistsDescriptor, TypeExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse TypeExists(ITypeExistsRequest TypeRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> TypeExistsAsync(Func<TypeExistsDescriptor, TypeExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> TypeExistsAsync(ITypeExistsRequest TypeRequest)
        {
            throw new NotImplementedException();
        }

        public IExplainResponse<T> Explain<T>(Func<ExplainDescriptor<T>, ExplainDescriptor<T>> querySelector) where T : class
        {
            throw new NotImplementedException();
        }

        public IExplainResponse<T> Explain<T>(IExplainRequest explainRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExplainResponse<T>> ExplainAsync<T>(Func<ExplainDescriptor<T>, ExplainDescriptor<T>> querySelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExplainResponse<T>> ExplainAsync<T>(IExplainRequest explainRequest) where T : class
        {
            throw new NotImplementedException();
        }

        public IMultiPercolateResponse MultiPercolate(Func<MultiPercolateDescriptor, MultiPercolateDescriptor> multiPercolateSelector)
        {
            throw new NotImplementedException();
        }

        public IMultiPercolateResponse MultiPercolate(IMultiPercolateRequest multiRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiPercolateResponse> MultiPercolateAsync(Func<MultiPercolateDescriptor, MultiPercolateDescriptor> multiPercolateSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IMultiPercolateResponse> MultiPercolateAsync(IMultiPercolateRequest multiPercolateRequest)
        {
            throw new NotImplementedException();
        }

        public IGetFieldMappingResponse GetFieldMapping<T>(Func<GetFieldMappingDescriptor<T>, GetFieldMappingDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public IGetFieldMappingResponse GetFieldMapping(IGetFieldMappingRequest getFieldMappingRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetFieldMappingResponse> GetFieldMappingAsync<T>(Func<GetFieldMappingDescriptor<T>, GetFieldMappingDescriptor<T>> selector = null) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetFieldMappingResponse> GetFieldMappingAsync(IGetFieldMappingRequest getFieldMappingRequest)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse TemplateExists(Func<TemplateExistsDescriptor, TemplateExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse TemplateExists(ITemplateExistsRequest templateRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> TemplateExistsAsync(Func<TemplateExistsDescriptor, TemplateExistsDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> TemplateExistsAsync(ITemplateExistsRequest templateRequest)
        {
            throw new NotImplementedException();
        }

        public IPingResponse Ping(Func<PingDescriptor, PingDescriptor> pingSelector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPingResponse> PingAsync(Func<PingDescriptor, PingDescriptor> pingSelector = null)
        {
            throw new NotImplementedException();
        }

        public virtual IPingResponse Ping(IPingRequest pingRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPingResponse> PingAsync(IPingRequest pingRequest)
        {
            throw new NotImplementedException();
        }

        public ISearchShardsResponse SearchShards<T>(Func<SearchShardsDescriptor<T>, SearchShardsDescriptor<T>> searchSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public ISearchShardsResponse SearchShards(ISearchShardsRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchShardsResponse> SearchShardsAsync<T>(Func<SearchShardsDescriptor<T>, SearchShardsDescriptor<T>> searchSelector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISearchShardsResponse> SearchShardsAsync(ISearchShardsRequest request)
        {
            throw new NotImplementedException();
        }

        public IGetRepositoryResponse GetRepository(Func<GetRepositoryDescriptor, GetRepositoryDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public IGetRepositoryResponse GetRepository(IGetRepositoryRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetRepositoryResponse> GetRepositoryAsync(Func<GetRepositoryDescriptor, GetRepositoryDescriptor> selector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetRepositoryResponse> GetRepositoryAsync(IGetRepositoryRequest request)
        {
            throw new NotImplementedException();
        }

        public ISnapshotStatusResponse SnapshotStatus(Func<SnapshotStatusDescriptor, SnapshotStatusDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ISnapshotStatusResponse SnapshotStatus(ISnapshotStatusRequest getSnapshotRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISnapshotStatusResponse> SnapshotStatusAsync(Func<SnapshotStatusDescriptor, SnapshotStatusDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ISnapshotStatusResponse> SnapshotStatusAsync(ISnapshotStatusRequest getSnapshotRequest)
        {
            throw new NotImplementedException();
        }

        public IRecoveryStatusResponse RecoveryStatus(Func<RecoveryStatusDescriptor, RecoveryStatusDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IRecoveryStatusResponse RecoveryStatus(IRecoveryStatusRequest statusRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRecoveryStatusResponse> RecoveryStatusAsync(Func<RecoveryStatusDescriptor, RecoveryStatusDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IRecoveryStatusResponse> RecoveryStatusAsync(IRecoveryStatusRequest statusRequest)
        {
            throw new NotImplementedException();
        }

        public Elasticsearch.Net.ElasticsearchResponse<T> DoRequest<T>(string method, string path, object data = null, Elasticsearch.Net.IRequestParameters requestParameters = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<Elasticsearch.Net.ElasticsearchResponse<T>> DoRequestAsync<T>(string method, string path, object data = null, Elasticsearch.Net.IRequestParameters requestParameters = null)
        {
            throw new NotImplementedException();
        }

        public IPutScriptResponse PutScript(Func<PutScriptDescriptor, PutScriptDescriptor> putScriptDescriptor)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IPutScriptResponse> PutScriptAsync(Func<PutScriptDescriptor, PutScriptDescriptor> putScriptDescriptor)
        {
            throw new NotImplementedException();
        }

        public IGetScriptResponse GetScript(Func<GetScriptDescriptor, GetScriptDescriptor> getScriptDescriptor)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetScriptResponse> GetScriptAsync(Func<GetScriptDescriptor, GetScriptDescriptor> getScriptDescriptor)
        {
            throw new NotImplementedException();
        }

        public IDeleteScriptResponse DeleteScript(Func<DeleteScriptDescriptor, DeleteScriptDescriptor> deleteScriptDescriptor)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IDeleteScriptResponse> DeleteScriptAsync(Func<DeleteScriptDescriptor, DeleteScriptDescriptor> deleteScriptDescriptor)
        {
            throw new NotImplementedException();
        }

        public IGetIndexResponse GetIndex(Func<GetIndexDescriptor, GetIndexDescriptor> getIndexSelector)
        {
            throw new NotImplementedException();
        }

        public IGetIndexResponse GetIndex(IGetIndexRequest createIndexRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetIndexResponse> GetIndexAsync(Func<GetIndexDescriptor, GetIndexDescriptor> getIndexSelector)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IGetIndexResponse> GetIndexAsync(IGetIndexRequest createIndexRequest)
        {
            throw new NotImplementedException();
        }

        public IExistsResponse SearchExists<T>(Func<SearchExistsDescriptor<T>, SearchExistsDescriptor<T>> selector) where T : class
        {
            throw new NotImplementedException();
        }

        public IExistsResponse SearchExists(ISearchExistsRequest indexRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> SearchExistsAsync<T>(Func<SearchExistsDescriptor<T>, SearchExistsDescriptor<T>> selector) where T : class
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IExistsResponse> SearchExistsAsync(ISearchExistsRequest indexRequest)
        {
            throw new NotImplementedException();
        }

        public IVerifyRepositoryResponse VerifyRepository(string name, Func<VerifyRepositoryDescriptor, VerifyRepositoryDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public IVerifyRepositoryResponse VerifyRepository(IVerifyRepositoryRequest verifyRepositoryRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IVerifyRepositoryResponse> VerifyRepositoryAsync(string name, Func<VerifyRepositoryDescriptor, VerifyRepositoryDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IVerifyRepositoryResponse> VerifyRepositoryAsync(IVerifyRepositoryRequest verifyRepositoryRequest)
        {
            throw new NotImplementedException();
        }

        public IUpgradeResponse Upgrade(IUpgradeRequest upgradeRequest)
        {
            throw new NotImplementedException();
        }

        public IUpgradeResponse Upgrade(Func<UpgradeDescriptor, UpgradeDescriptor> upgradeDescriptor = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpgradeResponse> UpgradeAsync(IUpgradeRequest upgradeRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpgradeResponse> UpgradeAsync(Func<UpgradeDescriptor, UpgradeDescriptor> upgradeDescriptor = null)
        {
            throw new NotImplementedException();
        }

        public IUpgradeStatusResponse UpgradeStatus(IUpgradeStatusRequest upgradeRequest)
        {
            throw new NotImplementedException();
        }

        public IUpgradeStatusResponse UpgradeStatus(Func<UpgradeStatusDescriptor, UpgradeStatusDescriptor> upgradeDescriptor = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpgradeStatusResponse> UpgradeStatusAsync(IUpgradeStatusRequest upgradeRequest)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<IUpgradeStatusResponse> UpgradeStatusAsync(Func<UpgradeStatusDescriptor, UpgradeStatusDescriptor> upgradeDescriptor = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatAliasesRecord> CatAliases(Func<CatAliasesDescriptor, CatAliasesDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatAliasesRecord> CatAliases(ICatAliasesRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatAliasesRecord>> CatAliasesAsync(Func<CatAliasesDescriptor, CatAliasesDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatAliasesRecord>> CatAliasesAsync(ICatAliasesRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatAllocationRecord> CatAllocation(Func<CatAllocationDescriptor, CatAllocationDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatAllocationRecord> CatAllocation(ICatAllocationRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatAllocationRecord>> CatAllocationAsync(Func<CatAllocationDescriptor, CatAllocationDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatAllocationRecord>> CatAllocationAsync(ICatAllocationRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatCountRecord> CatCount(Func<CatCountDescriptor, CatCountDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatCountRecord> CatCount(ICatCountRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatCountRecord>> CatCountAsync(Func<CatCountDescriptor, CatCountDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatCountRecord>> CatCountAsync(ICatCountRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatHealthRecord> CatHealth(Func<CatHealthDescriptor, CatHealthDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatHealthRecord> CatHealth(ICatHealthRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatHealthRecord>> CatHealthAsync(Func<CatHealthDescriptor, CatHealthDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatHealthRecord>> CatHealthAsync(ICatHealthRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatIndicesRecord> CatIndices(Func<CatIndicesDescriptor, CatIndicesDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatIndicesRecord> CatIndices(ICatIndicesRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatIndicesRecord>> CatIndicesAsync(Func<CatIndicesDescriptor, CatIndicesDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatIndicesRecord>> CatIndicesAsync(ICatIndicesRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatMasterRecord> CatMaster(Func<CatMasterDescriptor, CatMasterDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatMasterRecord> CatMaster(ICatMasterRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatMasterRecord>> CatMasterAsync(Func<CatMasterDescriptor, CatMasterDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatMasterRecord>> CatMasterAsync(ICatMasterRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatNodesRecord> CatNodes(Func<CatNodesDescriptor, CatNodesDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatNodesRecord> CatNodes(ICatNodesRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatNodesRecord>> CatNodesAsync(Func<CatNodesDescriptor, CatNodesDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatNodesRecord>> CatNodesAsync(ICatNodesRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatPendingTasksRecord> CatPendingTasks(Func<CatPendingTasksDescriptor, CatPendingTasksDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatPendingTasksRecord> CatPendingTasks(ICatPendingTasksRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatPendingTasksRecord>> CatPendingTasksAsync(Func<CatPendingTasksDescriptor, CatPendingTasksDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatPendingTasksRecord>> CatPendingTasksAsync(ICatPendingTasksRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatPluginsRecord> CatPlugins(Func<CatPluginsDescriptor, CatPluginsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatPluginsRecord> CatPlugins(ICatPluginsRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatPluginsRecord>> CatPluginsAsync(Func<CatPluginsDescriptor, CatPluginsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatPluginsRecord>> CatPluginsAsync(ICatPluginsRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatRecoveryRecord> CatRecovery(Func<CatRecoveryDescriptor, CatRecoveryDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatRecoveryRecord> CatRecovery(ICatRecoveryRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatRecoveryRecord>> CatRecoveryAsync(Func<CatRecoveryDescriptor, CatRecoveryDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatRecoveryRecord>> CatRecoveryAsync(ICatRecoveryRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatThreadPoolRecord> CatThreadPool(Func<CatThreadPoolDescriptor, CatThreadPoolDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatThreadPoolRecord> CatThreadPool(ICatThreadPoolRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatThreadPoolRecord>> CatThreadPoolAsync(Func<CatThreadPoolDescriptor, CatThreadPoolDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatThreadPoolRecord>> CatThreadPoolAsync(ICatThreadPoolRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatShardsRecord> CatShards(Func<CatShardsDescriptor, CatShardsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatShardsRecord> CatShards(ICatShardsRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatShardsRecord>> CatShardsAsync(Func<CatShardsDescriptor, CatShardsDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatShardsRecord>> CatShardsAsync(ICatShardsRequest request)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatFielddataRecord> CatFielddata(Func<CatFielddataDescriptor, CatFielddataDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public ICatResponse<CatFielddataRecord> CatFielddata(ICatFielddataRequest request)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatFielddataRecord>> CatFielddataAsync(Func<CatFielddataDescriptor, CatFielddataDescriptor> selector = null)
        {
            throw new NotImplementedException();
        }

        public System.Threading.Tasks.Task<ICatResponse<CatFielddataRecord>> CatFielddataAsync(ICatFielddataRequest request)
        {
            throw new NotImplementedException();
        }

        public Elasticsearch.Net.Connection.IConnection Connection {
            get {
                throw new NotImplementedException();
            }
        }

        public INestSerializer Serializer {
            get {
                throw new NotImplementedException();
            }
        }

        public Elasticsearch.Net.IElasticsearchClient Raw {
            get {
                throw new NotImplementedException();
            }
        }

        public ElasticInferrer Infer {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

