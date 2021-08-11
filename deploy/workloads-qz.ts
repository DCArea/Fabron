import * as pulumi from "@pulumi/pulumi";
import { ConfigMap, Secret, Service, ServiceSpecType } from "@pulumi/kubernetes/core/v1";
import { image_registry, namespace_name, shared_labels } from "./core";
import { Deployment } from "@pulumi/kubernetes/apps/v1";
import { Ingress } from "@pulumi/kubernetes/networking/v1beta1";
import { PgSQLConfig } from "./pgsql";

const service_name = "qztester";
const image_name_qz = "qztester"
const app_name_api = "qztester";
const host = "qz.s.ichnb.com";

const image_version = process.env["IMAGE_VERSION"];
if (!image_version) { throw "missing IMAGE_VERSION" };
const image = `${image_registry}/${image_name_qz}:${image_version}`;

export function deploy(pgsql_config: PgSQLConfig) {
    const secret = deploy_secret(pgsql_config);
    const configmap = deploy_configmap();
    const { deployment, service } = deploy_app(app_name_api, image, configmap, secret);
    deploy_ingress(service);
    return { deployment, service }
}

function deploy_secret(pgsql_config: PgSQLConfig) {
    const secret = new Secret(service_name, {
        metadata: {
            namespace: namespace_name,
            name: service_name,
            labels: shared_labels
        },
        type: "Opaque",
        stringData: {
            "PgSQLConnectionString": pulumi.interpolate`Host=${pgsql_config.host};Port=${pgsql_config.port};Database=qz;Username=postgres;password=${pgsql_config.password};Maximum Pool Size=200`,
        }
    });
    return secret;
}

function deploy_configmap() {
    const configmap = new ConfigMap(service_name, {
        metadata: {
            namespace: namespace_name,
            name: service_name,
            labels: shared_labels
        },
        data: {
            "MaxConcurrency": "100",
            "MaxBatchSize": "5",
        }
    });
    return configmap;
}

function deploy_app(app_name: string, image_name: string, configmap: ConfigMap, secret: Secret) {
    const labels: { [key: string]: string } = {
        app: app_name,
        ...shared_labels
    };
    const deployment = new Deployment(app_name, {
        metadata: {
            name: app_name,
            namespace: namespace_name,
            labels: labels
        },
        spec: {
            replicas: 6,
            selector: {
                matchLabels: labels
            },
            template: {
                metadata: {
                    labels: labels,
                },
                spec: {
                    containers: [{
                        name: app_name,
                        image: image_name,
                        ports: [{
                            name: 'http',
                            containerPort: 80
                        }],
                        livenessProbe: {
                            httpGet: {
                                path: "/health",
                                port: 80
                            },
                            failureThreshold: 10,
                            timeoutSeconds: 30,
                        },
                        readinessProbe: {
                            httpGet: {
                                path: "/health",
                                port: 80
                            },
                            failureThreshold: 10,
                            timeoutSeconds: 30,
                        },
                        env: [{
                            name: "ASPNETCORE_ENVIRONMENT",
                            value: pulumi.getStack()
                        }, {
                            name: "DOTNET_SHUTDOWNTIMEOUTSECONDS",
                            value: "120"
                        }],
                        envFrom: [{
                            secretRef: { name: secret.metadata.name }
                        }, {
                            configMapRef: { name: configmap.metadata.name }
                        }],
                    }],
                }
            }
        }
    });

    const service = new Service(app_name, {
        metadata: {
            name: app_name,
            namespace: namespace_name,
            labels: {
                app: app_name,
                ...shared_labels
            }
        },
        spec: {
            ports: [{
                name: "http",
                port: 80
            }],
            selector: deployment.spec.template.metadata.labels,
            type: ServiceSpecType.ClusterIP
        }
    });

    return { deployment, service };
}

function deploy_ingress(service: Service) {
    return new Ingress(app_name_api, {
        metadata: {
            name: app_name_api,
            namespace: namespace_name,
            labels: shared_labels,
            annotations: {
                "cert-manager.io/cluster-issuer": "letsencrypt"
            }
        },
        spec: {
            ingressClassName: "nginx",
            tls: [
                {
                    hosts: [host],
                    secretName: `${app_name_api}-tls-secret`
                }
            ],
            rules: [
                {
                    host: host,
                    http: {
                        paths: [
                            {
                                path: "/",
                                backend: {
                                    serviceName: service.metadata.name,
                                    servicePort: "http"
                                }
                            },
                        ],
                    },
                }
            ]
        }
    });
}

