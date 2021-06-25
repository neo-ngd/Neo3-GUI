/* eslint-disable */
import React from "react";
import { Layout, Menu,Input } from "antd";
import { Link } from "react-router-dom";
import MenuDown from "../Common/menudown";
import { HomeOutlined, FileSyncOutlined,ArrowRightOutlined, SearchOutlined } from "@ant-design/icons";
import { withTranslation } from "react-i18next";

const { Sider } = Layout;
const { SubMenu } = Menu;

@withTranslation()
class Contractlayout extends React.Component {
  render = () => {
    const { t } = this.props;
    return (
      <div style={{ height: "100%" }}>
        <Sider className="menu-logo" style={{ height: "100%" }}>
          <Menu
            className="menu-scroll"
            theme="light"
            defaultSelectedKeys={["1"]}
            defaultOpenKeys={["sub1"]}
            mode="inline"
          >
            <Menu.Item>
              <Link to="/">
                <HomeOutlined />
                {t("sideBar.home")}
              </Link>
            </Menu.Item>
            <SubMenu
              key="sub1"
              title={
                <span>
                  <FileSyncOutlined />
                  <span>{t("sideBar.contract")}</span>
                </span>
              }
            >
              <Menu.Item key="1">
                <Link to="/contract">{t("sideBar.search contract")}</Link>
              </Menu.Item>
              <Menu.Item key="2">
                <Link to="/contract/deploy">
                  {t("sideBar.deploy contract")}
                </Link>
              </Menu.Item>
              <Menu.Item key="3">
                <Link to="/contract/invoke">
                  {t("sideBar.invoke contract")}
                </Link>
              </Menu.Item>
               <Menu.Item key="4">
                <Link to="/contract/upgrade">
                  {t("sideBar.upgrade contract")}
                </Link>
              </Menu.Item>
            </SubMenu>
          </Menu>
          <MenuDown />
        </Sider>
      </div>
    );
  };
}

export default Contractlayout;
