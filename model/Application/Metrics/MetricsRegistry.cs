using Prometheus;

namespace Application.Metrics
{
    public static class MetricsRegistry
    {
        public static readonly Gauge PackagesInQueue = 
            Prometheus.Metrics.CreateGauge("number_of_packages_in_queue", 
                "Number of packages currently in the input folder.");
        
        public static readonly Gauge NotMappedPackages = 
            Prometheus.Metrics.CreateGauge("number_of_packages_waiting", 
                "Number of packages that are currently waiting for mapping data on GraceNote.");
        
        public static readonly Counter PackagesSuccessfullyProcessed = 
            Prometheus.Metrics.CreateCounter("number_of_packages_successfully_processed", 
                "Number of packages that were successfully processed.");
        
        public static readonly Counter PackagesFailedToProcess = 
            Prometheus.Metrics.CreateCounter("number_of_packages_failed_to_process", 
                "Number of packages for which processing failed.");
    }
}