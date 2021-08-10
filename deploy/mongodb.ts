import * as pulumi from "@pulumi/pulumi";
import * as k8s from "@pulumi/kubernetes";

const name = "mongodb";
export interface MongoDbConfig {
    host: pulumi.Output<string>;
    port: pulumi.Output<number>;
}
export function deploy(namespace_name: string): MongoDbConfig {
    const mongodb = new k8s.helm.v3.Chart(name, {
        chart: "mongodb",
        fetchOpts: {
            repo: "https://charts.bitnami.com/bitnami"
        },
        version: "10.23.9",
        namespace: namespace_name,
        values: {
            architecture: "standalone",
            global: {
                storageClass: "alicloud-disk-ssd",
            },
            persistence:{
                size: "20Gi"
            },
            auth:{
                enabled: false
            }
        }
    });
    const mongodb_svc = mongodb.getResource("v1/Service", namespace_name, "mongodb");
    return {
        host: pulumi.interpolate`${mongodb_svc.metadata.name}.${namespace_name}.svc.cluster.local`,
        port: mongodb_svc.spec.ports[0].port,
    };
}
