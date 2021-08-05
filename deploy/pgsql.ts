import * as pulumi from "@pulumi/pulumi";
import * as k8s from "@pulumi/kubernetes";
import * as random from "@pulumi/random";

const name = "pgsql";
export interface PgSQLConfig {
    host: pulumi.Output<string>;
    port: pulumi.Output<number>;
    password: pulumi.Output<string>;
}
export function deploy(namespace_name: string): PgSQLConfig {
    const pgsql_password = new random.RandomPassword("pgsql_password", {
        length: 8,
        special: false
    });
    const redis = new k8s.helm.v3.Chart(name, {
        chart: "postgresql",
        fetchOpts: {
            repo: "https://charts.bitnami.com/bitnami"
        },
        version: "10.6.1",
        namespace: namespace_name,
        values: {
            global: {
                storageClass: "alicloud-disk-ssd",
                postgresql: {
                    postgresqlPassword: pgsql_password.result
                }
            },
            persistence:{
                size: "20Gi"
            },
            postgresqlMaxConnections: "2000"
        }
    });
    const pgsql_svc = redis.getResource("v1/Service", namespace_name, "pgsql-postgresql");
    return {
        host: pulumi.interpolate`${pgsql_svc.metadata.name}.${namespace_name}.svc.cluster.local`,
        port: pgsql_svc.spec.ports[0].port,
        password: pgsql_password.result
    };
}
