import { Service } from "@pulumi/kubernetes/core/v1";
import { ServiceMonitor } from "./prometheus-operator-crds/monitoring/v1";

export function deploy(name: string, service: Service) {
    return new ServiceMonitor(name, {
        metadata: {
            name: service.metadata.name,
            namespace: service.metadata.namespace,
            labels: service.metadata.labels
        },
        spec: {
            selector: {
                matchLabels: service.metadata.labels
            },
            endpoints: [{
                port: "http",
                path: "/metrics",
                interval: "5s"
            }]
        }
    });
}
