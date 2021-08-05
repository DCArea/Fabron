import * as pulumi from "@pulumi/pulumi";
import * as k8s from "@pulumi/kubernetes";
import * as random from "@pulumi/random";

const name = "redis";
export interface RedisConfig {
    host: pulumi.Output<string>;
    port: pulumi.Output<number>;
    password: pulumi.Output<string>;
}
export function deploy(namespace_name: string): RedisConfig {
    const redis_password = new random.RandomPassword("redis_password", {
        length: 8,
        special: false
    });
    const redis = new k8s.helm.v3.Chart(name, {
        chart: name,
        fetchOpts: {
            repo: "https://charts.bitnami.com/bitnami"
        },
        version: "14.1.1",
        namespace: namespace_name,
        values: {
            global: {
                redis: {
                    password: redis_password.result
                }
            },
            architecture: "standalone",
            master: {
                persistence: { enabled: false }
            }
        }
    });
    const redis_svc = redis.getResource("v1/Service", namespace_name, "redis-master");
    return {
        host: pulumi.interpolate`${redis_svc.metadata.name}.${namespace_name}.svc.cluster.local`,
        port: redis_svc.spec.ports[0].port,
        password: redis_password.result
    };
}
