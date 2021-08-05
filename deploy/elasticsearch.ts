import * as pulumi from "@pulumi/pulumi";
import * as k8s from "@pulumi/kubernetes";
import * as random from "@pulumi/random";

const name = "elasticsearch";
export interface ElasticSearchConfig {
    host: pulumi.Output<string>;
    port: pulumi.Output<number>;
}
export function deploy(namespace_name: string): ElasticSearchConfig {
    const redis = new k8s.helm.v3.Chart(name, {
        chart: "elasticsearch",
        fetchOpts: {
            repo: "https://helm.elastic.co"
        },
        version: "7.14.0",
        namespace: namespace_name,
        values: {
            image: "dcarea/elasticsearch",
            esJavaOpts: "-Xmx256m -Xms256m",
            resources: {
                requests: {
                    cpu: "300m",
                    memory: "512M"
                },
                limits: {
                    cpu: "1000m",
                    memory: "1024M"
                }
            },
            volumeClaimTemplate: {
                storageClassName: "alicloud-disk-ssd",
                resources: {
                    requests: {
                        storage: "30Gi"
                    }
                }
            }
        }
    });
    const es_svc = redis.getResource("v1/Service", namespace_name, "elasticsearch-master");
    return {
        host: pulumi.interpolate`${es_svc.metadata.name}.${namespace_name}.svc.cluster.local`,
        port: es_svc.spec.ports[0].port,
    };
}
